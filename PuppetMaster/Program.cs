﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppetMaster
{
    class Program
    {
        static void Main(string[] args)
        {
            ProcessLaucher processLaucher = new ProcessLaucher();
            Console.WriteLine("Welcome!");
            String text = Console.ReadLine();
            while (!text.Equals("exit"))
            {
                if (text.Split()[0].Equals("StartClient"))
                {
                    if(text.Split().Length ==6 && text.Split().Length == 7)
                        processLaucher.startClient(text.Split()[1], text.Split()[2], text.Split()[3], text.Split()[4], text.Split()[5], text.Split()[6]);
                    else
                        Console.WriteLine("StartClient PID PCS_URL CLIENT_URL MSEC_PER_ROUND NUM_PLAYERS [filename]");
                }
                else if(text.Split()[0].Equals("StartServer"))
                {
                    if (text.Split().Length == 6)
                        processLaucher.startServer(text.Split()[1], text.Split()[2], text.Split()[3], text.Split()[4], text.Split()[5]);
                    else
                        Console.WriteLine("StartServer PID PCS_URL SERVER_URL MSEC_PER_ROUND NUM_PLAYERS");
                }
                else if (text.Split()[0].Equals("GlobalStatus"))
                {
                    Console.WriteLine(text);
                }
                else if (text.Split()[0].Equals("Crash"))
                {
                    Console.WriteLine(text);
                }
                else if (text.Split()[0].Equals("Freeze"))
                {
                    Console.WriteLine(text);
                }
                else if (text.Split()[0].Equals("Unfreeze"))
                {
                    Console.WriteLine(text);
                }
                else if (text.Split()[0].Equals("InjectDelay"))
                {
                    Console.WriteLine(text);
                }
                else if (text.Split()[0].Equals("LocalState"))
                {
                    Console.WriteLine(text);
                }
                




                Console.WriteLine(text);
                text = Console.ReadLine();
            }
        }
    }
}