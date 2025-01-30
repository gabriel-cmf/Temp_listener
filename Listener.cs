using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection.Metadata;
using Newtonsoft.Json;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Web;
using CsvHelper.Configuration.Attributes;
using System.Linq;
using System.Net.NetworkInformation;

namespace Temp_listener_forms;


struct pos_entry
{
    [Name("nome")]
    public string nome { get; set; }
    [Name("pos")]
    public int pos { get; set; }

}
public class Listener
{
	Socket sc;
	IPEndPoint ep;
    HttpListener htl;
    bool running;
    byte[] buffer = new byte[1_024];
    int? raw_resp;
    Dictionary<string, int> pos_by_name;
    bool is_offline;
    public bool kill_connections;
    public bool rebuild_socket;
    public IPAddress ping_target;
    public List<Task> tasks;

    public Listener()
	{
        kill_connections=false;
        is_offline =false;
        rebuild_socket=false;
        sc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        sc.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        ping_target = IPAddress.Parse("200.235.173.1");
        htl = new();


        foreach(IPAddress ip in Dns.GetHostAddresses(Dns.GetHostName()))
        {
            Console.WriteLine(ip);
        }
        IPAddress hostIP = Dns.GetHostAddresses(Dns.GetHostName())[1];
        //IPEndPoint ep = new IPEndPoint(hostIP, 3335);
        ep = new IPEndPoint(hostIP, 3335);
        Console.WriteLine(ep);
        sc.Bind(ep);
        //sc.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        pos_by_name = new();
        raw_resp =null;
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            NewLine = Environment.NewLine,
        };


        using (var reader = new StreamReader("config/pcnametopos.txt"))
        {
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<pos_entry>();
                Console.WriteLine("records size");
                //Console.WriteLine(records.Count());
                //Console.WriteLine(records.First());
                //Console.WriteLine(records.ToHashSet().Count());
                //Console.WriteLine(records.ToList().Count());
                
                //List<pos_entry> l_r  = records.ToList();


                foreach (var p in records)
                {
                    Console.WriteLine(p.nome);
                    pos_by_name[p.nome] = p.pos;
                }
            }
        }
        Console.WriteLine("Values");
        Console.WriteLine(pos_by_name.Values.Count);
        tasks=new();






    }

    public async void receiver_wait()
    {
        Socket handle = sc.Accept();
        raw_resp = await handle.ReceiveAsync(buffer);
        handle.Close();
    }

    

  

    public void main_loop2(Form1 mf)
    {
        if (running)
        {
            Console.WriteLine("Not running again");
            return;
        }


       
        

        

        Console.WriteLine("Enter loop");
        running = true;
        //Socket handle = sc.Accept();
        CancellationToken token = new CancellationToken(mf.closed);
        CancellationToken panel_token = new CancellationToken(mf.closed);
        /*if (sc.Poll(1000, SelectMode.SelectRead) == true)
        {
            Console.WriteLine("Data pending");
        }*/
        sc.Listen(1000);
        byte[] b = new byte[1024];
        Ping ping = new Ping();
        int i = 0;
        while (true)
        {



            i = 0; 
           while(i < tasks.Count)
           {

                if (tasks[i].IsCompleted )
                {
                    tasks.Remove(tasks[i]);
                    i = 0;
                    continue;
                }

                i++;
           }
           

            
            //Console.WriteLine(sc.Available);
            
           
            
            if (sc.Connected || sc.Available > 0||sc.Poll(1000, SelectMode.SelectRead) == true) 
            {
                Console.WriteLine("Polled");
                  for(int a=0;a<mf.panels.Count; a++)
                  {

                    
                    if (mf.panels[a].has_computer==false)
                    {
                        mf.panels[a].has_computer = true;
                        mf.panels[a].socket = sc.Accept();
                        mf.panels[a].token = panel_token;
                        if(mf.panels[a].socket.Connected==true)
                        {
                            
                            mf.panels[a].wait_for_update();
                        }

                        
                        
                        if(mf.panels.Exists(x=> x!=mf.panels[a] && x.has_computer==true && x.comp_name_label.Text != Custom_panel.def_name && x.comp_name_label.Text == mf.panels[a].comp_name_label.Text))
                        {
                            Console.WriteLine("Computer already exists!");
                            mf.panels[a].call_reset();
                            
                        }

                        

                        if (mf.panels[a].has_computer  && mf.panels[a].get_name() != Custom_panel.def_name)
                        {
                            int real_pos = pos_by_name[mf.panels[a].get_name()];
                            //Console.WriteLine("Real pos:");
                            //Console.WriteLine(real_pos);
                            //Console.WriteLine("Current pos:");
                            //Console.WriteLine(a);
                            if ( a != real_pos)
                            {
                                Console.WriteLine("Swap pos");
                                Point c = mf.panels[a].Location;
                                mf.panels[a].call_repos(mf.panels[real_pos].Location);
                                mf.panels[real_pos].call_repos(c);
                                mf.panels[a].swapped = true;
                                

                                var pl = mf.panels[a];
                                mf.panels[a] = mf.panels[real_pos];
                                mf.panels[real_pos] = pl;

                            }
                        }

                        break;
                    }
                  }

            }

            
            try
            {
                //Console.WriteLine("Trying ping");
                
                PingReply reply = ping.Send(ping_target, 1000);
                is_offline = !(reply.Status == IPStatus.Success);
                if(!is_offline)
                {

                    
                    kill_connections = false;
                    //Console.WriteLine("Not offline");
                    if (rebuild_socket==true)
                    {
                        Console.WriteLine("Socket rebuild");
                        sc.Dispose();
                        IPAddress hostIP = Dns.GetHostAddresses(Dns.GetHostName())[1];
                        //IPEndPoint ep = new IPEndPoint(hostIP, 3335);
                        ep = new IPEndPoint(hostIP, 3335);
                        sc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        sc.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                        sc.Bind(ep);
                        sc.Listen(1000);
                        rebuild_socket = false;
                        for (int a = 0; a < mf.panels.Count; a++) { mf.panels[a].BackColor = Color.Blue; }
                        //Custom_panel.def_name = "Computador não conectado";
                    }
                    
                }
                // Console.WriteLine("ping done");
                else {
                    Console.WriteLine(reply.RoundtripTime);
                    Console.WriteLine(reply.Status);
                }
            }
            catch
            {
                is_offline=true;
                //Console.WriteLine("ping failed");

            }

            if(is_offline && !kill_connections)
            {


                //mf = new Form1();
                //mf.Show();
                //kill_connections = true;
                Console.WriteLine("Offline, all will be reset");
                for (int a = 0; a < mf.panels.Count; a++)
                {

                    //while (mf.panels[a].waiting) { }
                    /*if (mf.panels[a].has_computer && mf.panels[a].socket.Connected)
                    {
                        Console.WriteLine("Disconnecting");
                        
                        mf.panels[a].shutdown = true;
                        mf.panels[a].call_reset();
                    }*/
                    /*if(mf.panels[a].has_computer)
                    {
                        mf.call_remove_panels(mf.panels[a]);
                        mf.panels[a] = new Custom_panel(mf);
                        //mf.call_add_panel(mf.panels[a]);
                        mf.call_create_panel_at(a);
                    }*/

                    var pl = mf.panels[a];
                    var t = Task.Run(() =>
                    {

                        pl.call_reset();
                    }, panel_token);

                    tasks.Add(t);

                    //mf.call_create_panel_at(a);
                    //mf.Text = "Monitor de temperatura SEM CONEÇÂO";
                    
                    mf.panels[a].BackColor = Color.Black;

                }
                



                if (sc.Connected)
                {
                    sc.Disconnect(true);
                }
                
                //sc.Dispose();
                kill_connections = true;
                rebuild_socket = true;

            }

            
            for (int a = 0; a < mf.panels.Count; a++)
            {
                mf.panels[a].call_shutdown();
                if(mf.panels[a].has_computer == true && mf.panels[a].waiting==false)
                {
                    var pl = mf.panels[a];
                    //Console.WriteLine(a);

                    var t = Task.Run(() =>
                    {

                        pl.wait_for_update();
                    },panel_token);

                    tasks.Add(t);
                    

                }
            }


            if (mf.closed == true)
            {
                Console.WriteLine("Loop break");
                System.Windows.Forms.Application.ExitThread();
                return;
            }
        }
    }

}
