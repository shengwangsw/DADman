﻿using ComLibrary;
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;

namespace Client
{
    public delegate void doWork(object sender, EventArgs e);
    public partial class ClientForm : Form
    {
        public int roundID = -1;
        public int myNumber = 1;

        string nickname;
        public int port;
        public bool started = false;
        bool dead = false;
        bool sent = false;
        public bool freeze = false;

        // direction player is moving in. Only one will be true
        bool goup;
        bool godown;
        bool goleft;
        bool goright;

        int boardRight = 320;
        int boardBottom = 320;
        int boardLeft = 0;
        int boardTop = 40;

        //player speed
        int speed = 5;
        int score = 0;
        int total_coins = 61;
        Dictionary<string, int> delayLog;
        ConnectedClient lider;

        private TcpChannel channel;

        public List<ConnectedClient> clients;

        public Dictionary<string, IServer> serversConnected = new Dictionary<string, IServer>();

        public Dictionary<int, BoardInfo> boardByRound;

        public Thread movePool;
        private List<Movement> listMove;
        private ThreadMove tlock;


        public ClientForm()
        {
          
            nickname = Program.PLAYERNAME;
            clients = new List<ConnectedClient>();
            delayLog = new Dictionary<string, int>();
            boardByRound = new Dictionary<int, BoardInfo>();
            InitializeComponent();
            this.Text += ": " + nickname;
            label2.Visible = false;

            listMove = new List<Movement>();
            tlock = new ThreadMove();
            movePool = new Thread(new ThreadStart(retrieveMove));
            movePool.Start();

            Init();

        }

        public void Init()
        {

            this.nickname = Program.PLAYERNAME;
            this.port = Program.PORT;
            channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, false);


            
            RemoteClient rmc = new RemoteClient(this);
            string clientServiceName = "Client";
            
            RemotingServices.Marshal(
                rmc,
                clientServiceName,
                typeof(RemoteClient)
            );
            try
            {
                if (Program.SERVERURL != "")
                {
                    string[] serv = Program.SERVERURL.Split('-');
                    for (int i = 1; i < serv.Length; i++)
                    {
                        connectToServer(serv[i].Split('_')[0], serv[i].Split('_')[1]);
                    }
                }
            }
            catch { tbChat.AppendText("Catched"); }

        }

        public void connectToServer(string servername, string serverURL)
        {
            IServer serverProxy = null;
            while (serverProxy == null)
            {

                try
                {
                    serverProxy = (IServer)Activator.GetObject(
                    typeof(IServer),
                    serverURL);

                    serverProxy.connectClient(nickname, "tcp://" + Util.GetLocalIPAddress() + ":" + port + "/Client");
                    serversConnected.Add(servername, serverProxy);
                    break;
                }
                catch
                {
                    tbChat.Text += "Catch: Didn't connected to server, trying again";
                    serverProxy = null;
                    Thread.Sleep(1000);
                }
            }
        }
        

        public void addMove(Movement move)
        {
            tlock.add(ref listMove, move);
        }
        public void retrieveMove()
        {
            while (true)
            {
                tlock.ret(ref listMove, ref roundID, ref dead, ref sent, ref serversConnected);
            }
        }
        
        private void keyisdown(object sender, KeyEventArgs e)
        {
            if (!sent && !freeze)
                if (started)
                {
                    if (!dead)
                    {
                        foreach (KeyValuePair<string, IServer> entry in serversConnected)
                        {
                            if (e.KeyCode == Keys.Left)
                            {
                                entry.Value.sendMove(new Movement(roundID, nickname, myNumber, "LEFT"));
                                sent = true;
                            }
                            if (e.KeyCode == Keys.Right)
                            {
                                entry.Value.sendMove(new Movement(roundID, nickname, myNumber, "RIGHT"));
                                sent = true;
                            }
                            if (e.KeyCode == Keys.Up)
                            {
                                entry.Value.sendMove(new Movement(roundID, nickname, myNumber, "UP"));
                                sent = true;
                            }
                            if (e.KeyCode == Keys.Down)
                            {
                                entry.Value.sendMove(new Movement(roundID, nickname, myNumber, "DOWN"));
                                sent = true;
                            }
                        }
                    }
                }
            if (e.KeyCode == Keys.Enter)
            {
                tbMsg.Enabled = true; tbMsg.Focus();
            }
        }

        private void keyisup(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left)
            {
                goleft = false;
            }
            if (e.KeyCode == Keys.Right)
            {
                goright = false;
            }
            if (e.KeyCode == Keys.Up)
            {
                goup = false;
            }
            if (e.KeyCode == Keys.Down)
            {
                godown = false;
            }
        }

        private void tbMsg_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Thread thread = new Thread(() =>
                {
                    processMessage();
                });
                thread.Start();
                tbMsg.Enabled = false;
                this.Focus();

            }
        }
        
        public void updateChat(string nick, string msg)
        {
        tbChat.AppendText("\r\n" +nick + ": " + msg);
        }
        
        public void updateMove(int playernumber, string move)
        {
            goleft = goright = goup = godown = false;
            PictureBox pb = getPictureBoxByName("pictureBoxPlayer" + playernumber);

            if (move.Equals("LEFT"))
            {
                if (pb.Left > (boardLeft))
                {
                    pb.Left -= speed;
                    pb.Image = Properties.Resources.Left;
                }
            }
            if (move.Equals("RIGHT"))
            {
                if (pb.Left < (boardRight))
                {
                    pb.Left += speed;
                    pb.Image = Properties.Resources.Right;
                }
            }
            if (move.Equals("UP"))
            {
                if (pb.Top > (boardTop))
                {
                    pb.Top -= speed;
                    pb.Image = Properties.Resources.Up;
                }
            }
            if (move.Equals("DOWN"))
            {
                if (pb.Top < (boardBottom))
                {
                    pb.Top += speed;
                    pb.Image = Properties.Resources.down;
                }
            }
            sent = false;
        }

        public void updateGhostsMove(int g1, int g2, int g3x, int g3y)
        {
            redGhost.Left = g1;
            yellowGhost.Left = g2;
            pinkGhost.Left = g3x;
            pinkGhost.Top = g3y;
        }

        internal void updateDead(int playerNumber)
        {
            Console.WriteLine(myNumber + playerNumber);
            if (myNumber == playerNumber)
            {
                dead = true;
                label2.Text = "GAME OVER";
                label2.Visible = true;
                getPictureBoxByName("pictureBoxPlayer" + myNumber).BackColor = Color.Black;
            }
        }

        internal void updateCoin(string pictureBoxName, string playernumber)
        {
            if (myNumber == Int32.Parse(playernumber))
            {
                this.score++;
                label1.Text = "score: " + score;
            }
            foreach (Control x in this.Controls)
            {
                if (x is PictureBox && x.Tag == "coin" && x.Name.Equals(pictureBoxName))
                {
                    Controls.Remove(x);
                }
            }
        }

        internal void startGame(int playerNumbers)
        {
            if (!started)
            {
                roundID = 1;
                for (int i = 2; i <= playerNumbers; i++)
                    getPictureBoxByName("pictureBoxPlayer" + i).Visible = true;
                started = true;

                getPictureBoxByName("pictureBoxPlayer" + myNumber).BackColor = Color.LightSkyBlue;
                Thread thread = new Thread((new ThreadStart(doWork)));
                thread.Start();
            }
        }

        private void doWork()
        {
            if (Program.FILENAME != "")
            {
                using (TextFieldParser parser = new TextFieldParser(Util.PROJECT_ROOT + "PuppetMaster" +
                    Path.DirectorySeparatorChar + "file" + Path.DirectorySeparatorChar + Program.FILENAME))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        try
                        {
                            addMove(new Movement(roundID, nickname, myNumber, fields[1]));
                        }
                        catch
                        {
                            tbChat.AppendText("\r\nOccur a error reading file");
                        }
                    }
                }
            }
        }
 
        
        private PictureBox getPictureBoxByName(string name)
        {
            foreach (object p in this.Controls)
            {
                if (p.GetType() == typeof(PictureBox))
                    if (((PictureBox)p).Name == name)
                        return (PictureBox)p;
            }
            return new PictureBox();
        }


        /*****************************************  chat methods  ***************************************************/


        private void processMessage()
        {
            try
            {
                if (!tbMsg.Text.Trim().Equals(""))
                {

                    string msg = tbMsg.Text;
                    tbMsg.Clear();
                    IClient myself = null;
                    int mId = 1;
                    if (lider == null)
                    {
                        Action act = () => {
                            takeLider(lider);
                        };
                        Thread thread = new Thread((new ThreadStart(act)));
                        thread.Start();
                        thread.Join();
                    }


                    Action act2 = () => {
                        mId = askId();
                    };
                    Thread thread2 = new Thread((new ThreadStart(act2)));
                    thread2.Start();
                    thread2.Join();

                    foreach (ConnectedClient connectedClient in clients)
                    {

                        if (delayLog.ContainsKey(connectedClient.nick) && connectedClient.connected)
                        {
                            Thread thread = new Thread(() => delayThread(msg, mId, connectedClient));
                            thread.Start();
                        }
                        else if (!connectedClient.nick.Equals(nickname) && connectedClient.connected)
                        {
                            Action act3 = () => {
                                try
                                {
                                    connectedClient.clientProxy.send(nickname, msg, mId);
                                }
                                catch (SocketException exception)
                                {
                                    connectedClient.connected = false;

                                }
                            };
                            Thread thread3 = new Thread((new ThreadStart(act3)));
                            thread3.Start();
                        }
                        else if (connectedClient.nick.Equals(nickname))
                        {
                            myself = connectedClient.clientProxy;
                        }

                    }
                    if (myself != null)
                    {
                        Action act4 = () =>
                        {
                            myself.send(nickname, msg, mId);
                        };

                        Thread thread4 = new Thread((new ThreadStart(act4)));
                        thread4.Start();

                    }

                }
                tbMsg.Clear();
            }
            catch (Exception exception)
            {
            }
        }

        public void injectDelay(string nick, int delay)
        {
            if (!delayLog.ContainsKey(nick))
            {
                delayLog.Add(nick, delay);
            }
            else
            {
                delayLog[nick] += delay;
            }
        }

        public void delayThread(string msg, int mId, ConnectedClient conn)
        {
            Thread.Sleep(delayLog[conn.nick]);
             try
                {
                conn.clientProxy.send(nickname, msg, mId);
                }
                catch (SocketException exception)
                {
                    conn.connected = false;

                }
        }

        /*****************************************  Lider methods  ***************************************************/


        public void updateLider(ConnectedClient conn)
        {
            lider = conn;
        }

        public void takeLider(ConnectedClient prev)
        {
            int next = 1;
            Boolean clOk = false;
            IClient myself = null;
            ConnectedClient liderCli = null;

            if (lider != null)
                next = lider.playernumber + 1;
            while (!clOk)
            {
                foreach (ConnectedClient connectedClient in clients)
                {
                    if (connectedClient.playernumber == next)
                    {
                        if (connectedClient.connected)
                            clOk = true;
                        else
                        {
                            if (next == clients.Count)
                                next = 1;
                            else
                                next++;
                        }
                    }
                }
            }

            foreach (ConnectedClient connectedClient in clients)
            {


                if (!connectedClient.nick.Equals(nickname) && connectedClient.connected)
                {
                    if (connectedClient.playernumber == next)
                        liderCli = connectedClient;
                    else
                    {
                        Action act3 = () =>
                        {
                            try
                            {
                                connectedClient.clientProxy.sendLider(next);
                            }
                            catch (SocketException exception)
                            {
                                connectedClient.connected = false;

                            }
                        };
                        Thread thread4 = new Thread((new ThreadStart(act3)));
                        thread4.Start();
                    }
                }
                else if (connectedClient.nick.Equals(nickname))
                {
                    myself = connectedClient.clientProxy;
                }

            }
            if (myself != null)
            {
                Action act2 = () => {
                    myself.sendLider(next);
                };
                Thread thread2 = new Thread((new ThreadStart(act2)));
                thread2.Start();
                thread2.Join();

            }
            if (liderCli != null)
            {
                Action act = () => {
                    try
                    {
                        liderCli.clientProxy.sendLider(next);
                    }
                    catch (SocketException exception)
                    {
                        liderCli.connected = false;
                        takeLider(liderCli);
                    }
                };
                Thread thread = new Thread((new ThreadStart(act)));
                thread.Start();
                thread.Join();

            }

        }

        public int askId()
        {
            int temp = 1;
            try
            {
                temp = lider.clientProxy.getId();
                return temp;
            }
            catch (Exception ex)
            {
                try
                {
                    lider.connected = false;
                    Action act = () =>
                    {
                        takeLider(lider);
                    };
                    Thread thread = new Thread((new ThreadStart(act)));
                    thread.Start();
                    thread.Join();

                    temp = lider.clientProxy.getId();
                    return temp;

                }catch(Exception exception)
                {

                }
            }
                return temp;

        }
        public BoardInfo getLocalState(int roundID)
        {
            return boardByRound[roundID];
        }

        public void debugFunction(string text)
        {
            tbChat.AppendText(text);
        }
    }
}
