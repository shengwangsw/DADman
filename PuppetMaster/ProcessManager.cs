﻿using ComLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppetMaster
{

    class ProcessManager
    {
        private ProcessLaucher processLaucher;
        
        public ProcessManager()
        {
            processLaucher = new ProcessLaucher();
            
            start();
        }

        public void start()
        {
            string text = "";
            while (!text.Equals("exit"))
            {
                text = Console.ReadLine();
                init(text);
                try
                {
                    
                    string inputFile = Util.PROJECT_ROOT+ "PuppetMaster" + Path.DirectorySeparatorChar+
                        "file" + Path.DirectorySeparatorChar + text.Split(' ')[0];

                    using (StreamReader sr = File.OpenText(inputFile))
                    {
                        string s = "";
                        while ((s = sr.ReadLine()) != null)
                        {
                            init(s);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Init Error...\r\n" + e.ToString());
                }

                
            }

            
        }

        public void init(string text)
        {

            if (text.Split(' ')[0].Equals("StartClient"))
            {
                processLaucher.startClient(text.Split(' '));
            }
            else if (text.Split(' ')[0].Equals("StartServer"))
            {
                processLaucher.startServer(text.Split(' '));
            }
            else if (text.Split(' ')[0].Equals("LocalState"))
            {
                processLaucher.checkLocalState(text.Split(' '));
            }
            else if (text.Split(' ')[0].Equals("GlobalStatus"))
            {
                processLaucher.checkGlobalState();
            }
            else if (text.Split(' ')[0].Equals("Crash"))
            {
                /* int pid = Int32.Parse(text.Split()[1]);
                 if (processes.ContainsValue(pid))
                 {

                     Process.GetProcessById(id).Kill();
                     var item = processes.First(kvp => kvp.Value == id);

                     processes.Remove(item.Key);
                 }*/
                Console.WriteLine(text);
            }
            else if (text.Split(' ')[0].Equals("Freeze"))
            {
                // https://stackoverflow.com/questions/71257/suspend-process-in-c-sharp
                int pid = Int32.Parse(text.Split(' ')[1]);
                processLaucher.freezeProcess(pid);

            }
            else if (text.Split(' ')[0].Equals("Unfreeze"))
            {
                int pid = Int32.Parse(text.Split(' ')[1]);
                processLaucher.unfreezeProcess(pid);
            }
            else if (text.Split(' ')[0].Equals("InjectDelay"))
            {
                //injectDelay(text.Split()[1], text.Split()[2]);
                Console.WriteLine(text);
            }
            else if (text.Split()[0].Equals("Wait"))
            {
                try
                {
                    Console.WriteLine("Wait: " + text.Split()[1] + " MSSEC");
                    System.Threading.Thread.Sleep(Int32.Parse(text.Split()[1]));
                }
                catch
                {
                    Console.WriteLine("Invalid MSSEC value...");
                }
            }

            else if (text.Split(' ')[0].Equals("Check"))
            {
                processLaucher.check();
            }
            /*
            else if (text.Split(' ')[0].Equals("ServerLog"))
            {
                if (File.Exists(pathLog))
                {
                    // Open the file to read from.

                    using (StreamReader sr = File.OpenText(pathLog))
                    {

                        string s = "";
                        while ((s = sr.ReadLine()) != null)
                        {
                            Console.WriteLine(s);
                        }
                    }
                }
            }
            */
        }

        

    }
}