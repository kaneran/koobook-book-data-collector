using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace KoobookServiceConsoleApp.TCP
{
    //Source: https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient?view=netframework-4.8
    class TCPClient
    {
        //This method works by connecting the the Python text summariser via TCP socket. After connecting, it sends the book description(passed into the method's arguments) to the Python text summariser which then summarises it.
        //After the summariser has completed the summarisation, it then sends it back to the client and waits until it recieves the "#" symbol which is the Python text sumamriser's way of informing the client and this is all
        //the data that it sent. After the client removes the "#" from the receives data string, the client should now have the summarised book description. It then initiates the partial handshake to close connection. After it
        //closes connection, it returns the summarised book description. If an exception was thrown during the main execution then the orginal book description(not summarised) will be returned by the method.  
        public string Connect(string bookDescription)
        {

            string summarisedBookDescription = null;
            try
            {
                string server = "192.168.1.252";
                Int32 port = 9878;
                TcpClient client = new TcpClient(server, port);

                Byte[] bookDescriptionData = Encoding.ASCII.GetBytes(bookDescription);

                NetworkStream stream = client.GetStream();

                stream.Write(bookDescriptionData, 0, bookDescriptionData.Length);
                Console.WriteLine("Sent {0} to Python text summariser", bookDescription);

                bookDescriptionData = new byte[256];


                StringBuilder sb = new StringBuilder();
                int i;
                bool dataReceived = false;




                //Receive summarised book data from Server(Python text summariser solution)
                while ((i = stream.Read(bookDescriptionData, 0, bookDescriptionData.Length)) != 0)
                {
                    sb.Append(Encoding.ASCII.GetString(bookDescriptionData, 0, i));
                    if (sb.ToString().Contains("#"))
                    {
                        sb.Replace("#", "");
                        break;
                    }

                }

                summarisedBookDescription = sb.ToString();

                //Partial handshake to close connection
                Byte[] data = Encoding.ASCII.GetBytes("FIN");
                stream.Write(data, 0, data.Length);

                bool finAcknowledged = false;
                int j;

                //Reset string builder
                sb.Length = 0;

                //Checks to see if Server has acknowledge that the cleint wants to close connection and also wants to close
                while ((j = stream.Read(data, 0, data.Length)) != 0 && finAcknowledged == false)
                {
                    sb.Append(Encoding.ASCII.GetString(data, 0, j));

                    if (sb.ToString().Contains("ACK") && sb.ToString().Contains("FIN"))
                    {
                        data = Encoding.ASCII.GetBytes("ACK");
                        stream.Write(data, 0, data.Length);
                    }
                    if (sb.ToString().Contains("CLOSED"))
                    {
                        finAcknowledged = true;
                    }
                }
                client.Close();
                return summarisedBookDescription;
            }
            catch (Exception e)
            {
                if (!String.IsNullOrEmpty(summarisedBookDescription))
                {
                    return summarisedBookDescription;
                }
                else
                {
                    return bookDescription;
                }
            }
        }
    }
}
