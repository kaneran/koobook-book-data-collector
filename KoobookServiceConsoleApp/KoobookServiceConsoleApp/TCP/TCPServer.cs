using KoobookServiceConsoleApp.Amazon;
using KoobookServiceConsoleApp.GoogleBooksApi;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KoobookServiceConsoleApp.TCP
{
    //Solution from https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener?view=netframework-4.8
    public class TCPServer
    {
        public bool dataContainsIsbn { get; set; }

        //This method works by initialising a port and creates a new TCP listener using that port. It then waits a client connects to it and it accepts it and then proceeds to read the data sent from the client(android application).
        //The server will contain to read until the Android application sends the "#" symbol to tell the server that it has received the entire data stirng. After removing the "#" symbol from the retreived data string, it then checks
        //it to see if the android application requested to retrieve information about one or several books. If the request is to get only one book then it uses the Book Data controller to get all relevent book information from
        //Goodreads, GoogleBooks and Amazon and the controller should return a single BookModel contaning the information. It then uses this model to concatanate all the information into a single string and sends it back to the android application.
        //It will also send a sub string "]d2C>^+" to tell the Android application(client) that it has sent all the book information.

        //If the android application requested to retrieve information about several books then it will collect the books data from Google books api and uses the Book controller to concantate all the books data into a single string 
        //and sends it back to the android application. It will also send a sub string "]d2C>^+" to tell the Android application(client) that it has sent all the book information.

        //After sending the information, it waits until the android application initiates the partial handshake to close connection. After it closes connection, it sleeps for 3 seconds and creates a new console window for the entire process to start again.

        //Credit to AWinkle for the idea on how to run tasks simultaneously https://codereview.stackexchange.com/questions/59147/running-2-sets-of-tasks-at-the-same-time
        public void Listen(string fileName)
        {
            TcpListener server = null;
            StringBuilder stringBuilder;
            string dataFromClient;
            try
            {
                Int32 port = 9876;
                server = new TcpListener(IPAddress.Any, port);
                server.Start();

                Byte[] bytes = new Byte[256];
                Byte[] isbnBytes = new byte[256];

                String data = null;
                stringBuilder = new StringBuilder();
                bool bookDataSentToAndroid = false;
                while (bookDataSentToAndroid == false)
                {

                    Console.WriteLine("Waiting for connection....");
                    TcpClient client = server.AcceptTcpClient();

                    Console.WriteLine("Connected!");
                    data = null;


                    NetworkStream stream = client.GetStream();

                    int i;
                    bool dataSent = false;
                    bool dataFromClientReceived = false;
                    bool ackReceived = false;

                    //Read data from client to receive all the bytes and append it together to prdouce the data 
                    while (dataFromClientReceived == false) {
                        i = stream.Read(isbnBytes, 0, isbnBytes.Length);
                        var mData = System.Text.Encoding.ASCII.GetString(isbnBytes, 0, i);
                        stringBuilder.Append(mData);
                        if (stringBuilder.ToString().Contains("#")) {
                            dataFromClientReceived = true;
                        }

                    }

                    dataFromClient = stringBuilder.ToString().Replace("#","");
                    Console.WriteLine("Received: {0}",dataFromClient);

                    //Check to see if the this solution will need collect data of one book or more than one book
                    //this is done by the client including a flag to determine the number of books as part of sending the message via the network stream
                    if (dataFromClient.Contains("!isMoreThanOneBook")) {

                        //Credit to DGibbs for solution on checking if string contains int
                        //if the string only contains numbers then the data received is the isbn
                        //otherwise, the data is the book title or author. For each case, a different method will be executed as the retrieve data will be returned in different formats
                        BookDataController bookDataController = new BookDataController();

                        dataFromClient = dataFromClient.Replace("!isMoreThanOneBook", "");
                        dataContainsIsbn = dataFromClient.All(char.IsDigit);

                        if (dataContainsIsbn.Equals(true))
                        {
                            //Collect book information using the Isbn and send it to the client
                            while (dataSent == false)
                            {
                                
                                bookDataController.CollectDataFromSources(dataFromClient);
                                var bookData = bookDataController.ConcatBookData();

                                //The reason why I added the sub string "]d2C>^+" was to allow the Android application to keep
                                //reading the data until it receives this substring which implies that the entire string has been read.
                                byte[] msg = System.Text.Encoding.ASCII.GetBytes(bookData + "]d2C>^+");
                                stream.Write(msg, 0, msg.Length);
                                
                                Console.WriteLine("Sent: {0} ", bookData+ "]d2C>^+");
                                dataSent = true;

                            }
                        }
                        else {

                            //Collect book information using the author/title and send it to the client
                            
                            while (dataSent == false)
                            {
                                bookDataController = new BookDataController();
                                bookDataController.CollectDataFromSources(dataFromClient);
                                var bookData = bookDataController.ConcatBookData();

                                byte[] msg = System.Text.Encoding.ASCII.GetBytes(bookData + "]d2C>^+");
                                stream.Write(msg, 0, msg.Length);
                                Console.WriteLine("Sent: {0} ", bookData + "]d2C>^+");
                                dataSent = true;
                            }
                        } 
                    }


                    
                    else {

                        dataFromClient = dataFromClient.Replace("isMoreThanOneBook", "");
                        //Collect the information about the books and send it to the client
                        while (dataSent == false)
                        {
                            var googleBooksApiKey = ConfigurationManager.AppSettings.Get("googleBooksApiKey");
                            GoogleBookApi googleBookApi = new GoogleBookApi(googleBooksApiKey);
                            BookDataController bookDataController = new BookDataController();

                            var books = googleBookApi.CollectDataForBooks(dataFromClient);
                            var bookData = bookDataController.ConcatBooksData(books);


                            //data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                            //Console.WriteLine("Received data{0}: ", data);

                            //data = data.ToUpper();

                            byte[] msg = System.Text.Encoding.ASCII.GetBytes(bookData + "]d2C>^+");
                            stream.Write(msg, 0, msg.Length);
                            Console.WriteLine("Sent: {0} ", bookData + "]d2C>^+");

                            //msg = System.Text.Encoding.ASCII.GetBytes("Genre of book");
                            //stream.Write(msg, 0, msg.Length);
                            //Console.WriteLine("Sent: {0} ", data);
                            dataSent = true;

                        }
                    }

                    

                    //Reads data from client to check if the client wants to close connection(FIN)
                    //or has acknowledged(ACK) that the server is ready to close connection
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0 && ackReceived == false)
                    {
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        Console.WriteLine("Received data{0}: ", data);

                        data = data.ToUpper();
                        if (data.Equals("FIN") || data.Equals("F"))
                        {
                            byte[] ack_msg = System.Text.Encoding.ASCII.GetBytes("ACK");
                            stream.Write(ack_msg, 0, ack_msg.Length);


                            byte[] fin_msg = System.Text.Encoding.ASCII.GetBytes("FIN");
                            stream.Write(fin_msg, 0, ack_msg.Length);

                        }
                        else if (data.Equals("ACK") || data.Equals("A"))
                        {
                            byte[] closed_msg = System.Text.Encoding.ASCII.GetBytes("CLOSED");
                            stream.Write(closed_msg, 0, closed_msg.Length);
                            ackReceived = true;
                            Thread.Sleep(1000);
                        }
                    }
                    bookDataSentToAndroid = true;
                    client.Close();

                    Thread.Sleep(3000);
                    System.Diagnostics.Process.Start(fileName);
                    Environment.Exit(0);

                }
            }
            catch (SocketException se)
            {
                Console.WriteLine(se);
            }
            finally
            {
                server.Stop();
                
            }

            Console.WriteLine("/n enter to continue");
            Console.Read();
        }


    }
}
