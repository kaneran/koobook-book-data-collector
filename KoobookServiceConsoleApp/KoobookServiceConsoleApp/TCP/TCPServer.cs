using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace KoobookServiceConsoleApp.TCP
{
    //Solution from https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener?view=netframework-4.8
    public class TCPServer
    {
        //This method initialises a port and creates a new TCP listener using that port
        //it then waits a client connects to it and it accepts it and then proceed to read and write to the client
        public void Listen()
        {
            TcpListener server = null;
            StringBuilder stringBuilder;
            string isbn;
            try
            {
                Int32 port = 9876;
                server = new TcpListener(IPAddress.Any, port);
                server.Start();

                Byte[] bytes = new Byte[256];
                Byte[] isbnBytes = new byte[256];

                String data = null;
                stringBuilder = new StringBuilder();

                while (true)
                {

                    Console.WriteLine("Waiting for connection....");
                    TcpClient client = server.AcceptTcpClient();

                    Console.WriteLine("Connected!");
                    data = null;


                    NetworkStream stream = client.GetStream();

                    int i;
                    bool dataSent = false;
                    bool ackReceived = false;

                    //Read data from client to receive all the bytes and append it together to prdouce the isbn number
                    while ((i = stream.Read(isbnBytes, 0, isbnBytes.Length)) != 0) {
                        var isbnData = System.Text.Encoding.ASCII.GetString(isbnBytes, 0, i);
                        stringBuilder.Append(isbnData);
                    }

                    isbn = stringBuilder.ToString();



                    //Send the book data to the client
                    while (dataSent == false)
                    {
                        //data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        //Console.WriteLine("Received data{0}: ", data);

                        //data = data.ToUpper();

                        byte[] msg = System.Text.Encoding.ASCII.GetBytes("Book data");
                        stream.Write(msg, 0, msg.Length);
                        Console.WriteLine("Sent: {0} ", data);

                        msg = System.Text.Encoding.ASCII.GetBytes("Genre of book");
                        stream.Write(msg, 0, msg.Length);
                        Console.WriteLine("Sent: {0} ", data);
                        dataSent = true;

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

                        }
                    }
                    client.Close();





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
