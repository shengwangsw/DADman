﻿using ComLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Server
{
    public delegate void delImageVisible(int playerNumber);
    //client in server
    class Client
    {
        public string nick;
        public int playernumber;
        public string url;
        public IClient clientProxy;
        // TODO defined at end
        public bool dead;
        public bool connected;
        public int score;
       
    }

    public class Server
    {
        private int MSECROUND = Program.MSSEC; //game speed [communication refresh time]

        static TcpChannel channel = new TcpChannel(Program.PORT);

        public static string PATH = @".."+ Path.DirectorySeparatorChar+".."+ Path.DirectorySeparatorChar+
            ".."+ Path.DirectorySeparatorChar+"Server"+ Path.DirectorySeparatorChar+
            "bin"+ Path.DirectorySeparatorChar+"Log.txt";

        public Server()
        {
            
            createConnection();
            
        }


        
        private void createConnection()
        {
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(RemoteServer),
                "Server",
                WellKnownObjectMode.Singleton
            );
        }

        
    }
    public class RemoteServer : MarshalByRefObject, IServer
    {
        internal List<Client> clientList = new List<Client>();
        private Dictionary<string, int> player_image_hashmap = new Dictionary<string, int>();
        public int numberPlayersConnected = 0;
        public delegate void delProcess(int playerNumber, string move);

        private string arg;

        public ServerForm serverForm;
        
        public RemoteServer()
        {
            Thread thread = new Thread(() => createServerForm());
            thread.Start();
        }


        private void createServerForm()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(serverForm = new ServerForm(this));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void connect(string nick, string url)
        {
            Client c = new Client();
            
            Console.WriteLine("URL: "+url);
            IClient clientProxy = (IClient)Activator.GetObject(
                typeof(IClient),
                url
            );

            numberPlayersConnected++;

            c.nick = nick;
            c.url = url;
            c.clientProxy = clientProxy;
            c.playernumber = numberPlayersConnected;
            c.dead = false;
            c.connected = true;
            c.score = 0;

            clientList.Add(c);
            
            //Creates a correspondence Nick - Player Number i.e. John - Player1

            assignPlayer(c);
            sendStartGame();
        }

        public void sendMove(string nick, string move)
        {
            /*
            int playerNumber=0;
            
            foreach(Client c in clientList)
            {
                if (c.nick.Equals(nick))
                    playerNumber = c.playernumber;
            }
            
            Console.WriteLine("player"+ playerNumber + ": " + nick + "\t receives: " + move);
            */
            int pl_number = player_image_hashmap[nick];

            try
            {
                this.serverForm.listMove.Add(pl_number, move);
            }
            catch (SocketException e)
            {
                Console.WriteLine("Send a Move Bug: " + e.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Client already sent a move in this round...\r\n");
            }

            //this.serverForm.Invoke(new delProcess(serverForm.processMove), new object[] { pl_number, move });
            

        }


        private void assignPlayer(Client c)
        {
            player_image_hashmap.Add(c.nick, c.playernumber);

            foreach (KeyValuePair<string, int> entry in player_image_hashmap)
            {
                Console.WriteLine("INFO: " + entry.Key + " is " + entry.Value);
            }

        }

        public void sendPlayerDead(int playerNumber)
        {
            
            
            foreach (Client c in clientList)
            {
                if (c.playernumber == playerNumber)
                {
                    c.dead = true;
                }
                try
                {
                    c.clientProxy.playerDead(playerNumber);
                }
                catch (SocketException e)
                {
                    c.connected = false;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception on server sendPlayerDead()");
                }
            }
            string nick = player_image_hashmap.FirstOrDefault(x => x.Value == playerNumber).Key;
            
        }

        public void sendCoinEaten(int playerNumber, string coinName)
        {
            foreach (Client c in clientList)
            {
                try
                {
                    c.clientProxy.coinEaten(playerNumber, coinName);
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Debug: " + e.ToString());
                }
                catch
                {
                    Console.WriteLine("Exception on server sendCoinEaten()");
                }

            }
            string nick = player_image_hashmap.FirstOrDefault(x => x.Value == playerNumber).Key;
            
        }

        //TODO problem here
        public void sendStartGame()
        {
            if (numberPlayersConnected == Program.PLAYERNUMBER) {
                arg = " ";
                Console.WriteLine("INTO IF - Client list number:"+ clientList.Count());
                foreach (Client c in clientList)
                {
                    arg += "-" +c.nick+":"+ c.playernumber + ":" + c.url;
                    
                }
                foreach (Client c in clientList)
                {
                    try
                    {
                        Console.WriteLine("foreach connecting: " + arg);
                        c.clientProxy.startGame(numberPlayersConnected, arg);
                        Console.WriteLine("foreach connected: " + arg);
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine("Socket Exception: " + e.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception: "+e.ToString());
                    }
                }
                
                Console.WriteLine("Game started!");
            }
            this.serverForm.Invoke(new delImageVisible(serverForm.startGame), new object[] { numberPlayersConnected });

        }
    }
}
