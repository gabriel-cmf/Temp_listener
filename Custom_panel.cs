using Newtonsoft.Json.Linq;
using System;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using LibreHardwareMonitor.Hardware;




namespace Temp_listener_forms;

public class Custom_panel : Panel
{

	public Custom_label comp_name_label;
    public Custom_label timestamp_label;
    public Custom_label runtime_label;
    public PictureBox comp_icon;


    public Socket socket;
	public int? resp;
	public CancellationToken token;
	byte[] buffer;
	public bool waiting;
    public bool swapped;
    public double worst_temp;

	public List<Custom_label> labels;
	public Font LargeFont;
	public static string def_name = "Computador não conectado";
	int slide_page;
	public Form1 owner;
	public DateTime boot_time;
	public bool shutdown;

    public bool has_computer;
	public List<string> used_keys;
	private delegate void SafeCallDelegate(Dictionary<string, object> dict);
	private delegate void SafeCallDelegateVoid();
    private delegate void pos_del(Point p);
    private delegate void AsyncCallDelegate();
    System.Drawing.Color none;
    System.Drawing.Color low;
    System.Drawing.Color med;
    System.Drawing.Color high;
    System.Drawing.Color ext;

    public System.Windows.Forms.Timer beep_cooldown;
    public bool can_beep;

    public Button next_btn;
	public bool? has_boot_time;
	


    public Custom_panel(Form1 o)
	{

		owner = o;
        shutdown=false;
        boot_time = new();
		has_boot_time = null;

		none = Color.Blue;
        low =Color.Green;
		med = System.Drawing.Color.FromArgb(193, 156, 0);
        high = Color.DarkRed;
		ext = Color.Red;

		beep_cooldown = new();
		beep_cooldown.Start();

		beep_cooldown.Interval = 20000;
		//beep_cooldown.Tick += reset_beep;

        resp = null;
		has_computer = false;
		this.BackColor = none;
		this.BorderStyle = BorderStyle.Fixed3D;

		LargeFont = new Font("Consolas", 14);


		comp_name_label = new Custom_label();
		comp_name_label.Text = def_name;
		comp_name_label.TextAlign = ContentAlignment.TopCenter;
		comp_name_label.AutoSize = false;

		comp_name_label.ForeColor = Color.White;
		//comp_name_label.ForeColor = System.Drawing.Color.FromArgb(255-none.R, 255 - none.G, 255 - none.B);
		comp_name_label.Height = 16;
		comp_name_label.Font = LargeFont;
		comp_name_label.Dock = DockStyle.Top;
		//comp_name_label.Size = new Size(comp_name_label.Size.Width, 14);
		//comp_name_label.BackColor = Color.White;
		comp_name_label.Update();
		comp_name_label.Location = new Point(0, 14);
		comp_name_label.MaximumSize = new Size(int.MaxValue, 20);
		//comp_name_label.Click += send_beep;


		timestamp_label = new();
		timestamp_label.Height = 16;
		timestamp_label.Font = LargeFont;
		timestamp_label.Dock = DockStyle.Bottom;
		timestamp_label.ForeColor =(Color)Color.White;
		timestamp_label.AutoSize = true;
		
		
        //timestamp_label.Location = new Point(0, this.Size.Height-40);
        //timestamp_label.MaximumSize = new Size(int.MaxValue, 20);
		timestamp_label.Text = "10:59:01";
        timestamp_label.Update();


		runtime_label = new();
		runtime_label.Height = 16;
		runtime_label.Font = LargeFont;
		//runtime_label.Dock= DockStyle.Bottom;
        
        runtime_label.ForeColor=(Color)Color.White;
		runtime_label .AutoSize = true;
		runtime_label.Text = "10:59:01";
		//runtime_label.Location.X = this.Size.Width- runtime_label.Location.X;
		//runtime_label.Location = new Point(this.Size.Width - runtime_label.Location.X, runtime_label.Location.Y);
		/*runtime_label.SetBounds(
			this.Bounds.Right - runtime_label.Bounds.Right,
			this.Bounds.Y,
			this.Bounds.Width,
			this.Bounds.Height);*/

		//runtime_label.BackColor = Color.White;
		//runtime_label.Left = (this.Bounds.Right - runtime_label.Bounds.Right)/2;
		//runtime_label.Left = (this.Bounds.Right- runtime_label.Right);
        runtime_label.Top += 184;
        runtime_label.Left += 194;

        //runtime_label.Top = (this.ClientSize.Height - runtime_label.Height) / 2;
        //runtime_label.TextAlign = ContentAlignment.BottomRight;
        runtime_label.Update();



        comp_icon = new PictureBox();

        
        comp_icon.Image = SystemIcons.GetStockIcon(StockIconId.DesktopPC).ToBitmap();
		

        comp_icon.SizeMode = PictureBoxSizeMode.AutoSize;
		comp_icon.Update();





        next_btn = new Button();

		//Console.WriteLine(this.Size.Width);
		//Console.WriteLine(next_btn.Size.Width);



		//System.Drawing.Icon a = SystemIcons.GetStockIcon(StockIconId.);

        next_btn.Font = LargeFont;
        next_btn.Text = ">";
        next_btn.Padding = new Padding(0, 0, 0, 0);
		next_btn.FlatStyle = FlatStyle.Flat;
		
		next_btn.FlatAppearance.BorderSize = 0;
        next_btn.FlatAppearance.MouseOverBackColor = this.BackColor;

        next_btn.Margin = new Padding(0, 0, 0, 0);
        next_btn.ForeColor = Color.White;
		//next_btn.TextAlign = ContentAlignment.MiddleCenter;

        next_btn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        next_btn.AutoSize = true;
		next_btn.Update();

        //next_btn.Size = new Size(8,20);
        next_btn.Location = new Point(256, 0);



        next_btn.Update();


        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		buffer = new byte[16384];



        //this.Controls.Add(gpu_temp_label);
        //this.Controls.Add(cpu_temp_label);


        this.Controls.Add(comp_icon);
        this.Controls.Add(next_btn);
        this.Controls.Add(comp_name_label);
        this.Controls.Add(runtime_label);
        this.Controls.Add(timestamp_label);
        


        next_btn.Hide();
		timestamp_label.Hide();
		comp_icon.Hide();
		runtime_label.Hide();

		System.Windows.Forms.ToolTip runtime_tooltip   = new();
        System.Windows.Forms.ToolTip name_tooltip = new();
        System.Windows.Forms.ToolTip timestamp_tooltip = new();


        runtime_tooltip.SetToolTip(runtime_label, "Tempo que o computador esta ligado");
		name_tooltip.SetToolTip(comp_name_label,"Nome do computador");
		timestamp_tooltip.SetToolTip(timestamp_label,"Hora que o pedido foi enviando segundo o computador");


        labels = new();
		used_keys = new List<string>();
		used_keys.Add("computer_name");


		this.Click += next_page;

        can_beep=true;





    }

	void reset()
	{
        //Console.WriteLine("Resetting panel");
        this.Click -= next_page;
		try
		{

            //Console.WriteLine("Try catch");
            this.BackColor = Color.Blue;

            foreach (Custom_label l in labels)
            {
                this.Controls.Remove(l);

            }

            //Console.WriteLine("Controls removed");

            if (socket != null)
            {
				//Console.WriteLine("Has socket");
                if (socket.Connected) { socket.Shutdown(SocketShutdown.Both);  socket.Close(); }
                //Console.WriteLine("Socket disconnected");
                socket.Dispose();
            }

            //Console.WriteLine("Socket disposed off ");

            labels.Clear();
            used_keys.Clear();
            used_keys.Add("computer_name");
            comp_name_label.Text = def_name;
            has_computer = false;
            waiting = false;
            next_btn.Hide();
            comp_icon.Hide();
            timestamp_label.Hide();
            runtime_label.Hide();
			has_boot_time = null;
            shutdown = false;
			slide_page = 0;
			//Console.WriteLine("Shutdown done");
        }
		catch (Exception ex) 
		{
			     Console.WriteLine(ex.ToString());
		}

		
	}

	public bool call_shutdown()
	{
		if(shutdown)
		{
			call_reset();
			return true;
		}
		return false;
	}

	public string get_name()
	{
		return comp_name_label.Text;

    }
	public void align()
	{
		comp_name_label.Size = this.Size;
    }
    public async void wait_for_update()
	{
		if (call_shutdown() ) { return; }
		Task<int>? asc = null; 
		try
		{
			waiting = true;
			asc = socket.ReceiveAsync(buffer);


			asc.Wait(token);


            if (asc != null && asc.IsCompletedSuccessfully)
            {
                resp = asc.Result;

                read_resp();
                waiting = false;
            }

			
        }
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			call_reset();
        }

		
        

    }

	

	public void read_resp()
	{
		
        if(resp != null)
		{
            var response = Encoding.UTF8.GetString(buffer, 0, (int)resp);

            Console.WriteLine("Responde length:");
            Console.WriteLine(response.Length);


			if(response.Length >= 21000)
			{
				Console.WriteLine("String to large");
				return;
			}

            if (response[0] != '{')
            {
                response = response.Split('{')[0] + "{";

            }

            if (response[response.Length - 1] != '}')
            {
                response += '}';

            }

			/*if((response.Count(x => x == '{')<=0 || response.Count(x => x == '}') <=0))
			{
                return;
            }*/
			int braces = response.Count(x => x == '{');

			if (braces >= 5) { return; }

            if (braces > 1)
			{
				//Console.WriteLine("cut");
				//int start_pos = response.IndexOf("}");
				//int end_pos = response.LastIndexOf("}");


                response =  response.Split('}')[1] + "}";


            }

			

            Console.WriteLine(response);
			Dictionary<string, object> dict = null;
            try
			{
                dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
            }

			catch 
			{
				return;
			}

            if(dict != null)
			{

				

				if (owner.pc_blacklist.Any() && owner.pc_blacklist.Contains((string)dict["computer_name"]))
				{

					has_computer = false;
					return;
				}


				if(get_name() == def_name)
				{
                    /*if(dict.Keys.Count<=0)
					{
						return;
					}
					if(dict.Keys.Count > owner.allowed_keys.Count)
					{
						for(int a=0;a<owner.allowed_keys.Count;a++)
						{
							if(!dict.Keys.Contains(owner.allowed_keys[a]))
							{
								return;
							}
						}
					}

					else
					{
                        foreach (string k in dict.Keys)
                        {
                           if(!owner.allowed_keys.Contains(k))
							{
								return;
							}
                        }
                    }*/
                    for (int a = 0; a < owner.allowed_keys.Count; a++)

					{
                        try
                        {

                            var r = dict[owner.allowed_keys[a]];


                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            dict[owner.allowed_keys[a]]=-1;
                        }
                    }

                    
				}
                label_update(dict);
            }
        }
    }


	public void call_reset()
	{
        if (this.InvokeRequired)
		{
            var d = new SafeCallDelegateVoid(reset);
            this.Invoke(d);
        }
		else
		{
			reset();
		}

    }
	public void label_update(Dictionary<string, object> dict)
	{
		Console.WriteLine("Label update");
		if(this.InvokeRequired)
		{
            var d = new SafeCallDelegate(update);
			this.Invoke(d,dict);

        }
		else
		{
			update(dict);
		}
    }


	void update_color()
	{
        if (this.BackColor == Color.Green || this.BackColor == med)
        {
            if (labels[slide_page].num_val > owner.temp_ranges[labels[slide_page].key][0])
            {
                chance_back_color(med);

            }


        }

        if (this.BackColor == med || this.BackColor == Color.Green)
        {
            if (labels[slide_page].num_val > owner.temp_ranges[labels[slide_page].key][1])
            {

                chance_back_color(high);
                //sb.Append(cpu_temp_label.Text);
                //sb.Append(" ◄===");
                //cpu_temp_label.Text = sb.ToString();

            }


        }

        if (this.BackColor == med || this.BackColor == Color.Green||this.BackColor == high)
		{
            if (labels[slide_page].num_val > owner.temp_ranges[labels[slide_page].key][1])
			{
				Extreme_temp_report ex = new(get_name(), labels[slide_page].num_val,DateTime.Now, labels[slide_page].key,1.0);
				owner.write_event(ex);
			}

        }

    }
	void update(Dictionary<string,object> dict)
	{


        if (call_shutdown()) { return; }
        has_computer = true;
		chance_back_color(Color.Green);
		//next_btn.Show();
        //worst_temp = Math.Max((double)dict["cpu_core_max"], (double)dict["gpu_hotspot"]);
        //Console.WriteLine(dict.Values.ElementAt(0));
        //Console.WriteLine(dict.Values.ElementAt(1));
        //Console.WriteLine(dict.Values.ElementAt(2));
        comp_name_label.Text = (string)dict["computer_name"];
        comp_icon.Show();
        //StringBuilder sb = new StringBuilder("Gpu hostspot: ");
        StringBuilder sb = new StringBuilder();
		/*sb.Append((double)dict["gpu_hotspot"]);
        gpu_temp_label.Text = sb.ToString();
		sb.Clear();*/

		/*if(comp_name_label.Text == "LABGEO07")
		{
			Console.WriteLine(dict.Keys);
		}*/

		if(has_boot_time==null)
		{
            try
            {
                var e = dict["boot_time"];
				has_boot_time = true;
				runtime_label.Show();
            }
            catch
            {
				has_boot_time=false;
            }
        }


		


		foreach(string k in dict.Keys)
		{
			
			//if(k == "gpu_hotspot") { k = "GPU Hot Spot"; }
            //if (k == "cpu_core_max") { k = "Core Max"; }
            var dt = dict[k] as double?;
			if(owner != null && dt != null && owner.is_key_allowed(k)  && !used_keys.Contains(k) )
			{
				used_keys.Add(k);
				

                create_label(k, (double)dict[k], owner);

				if(labels.Count ==1)
				{
					labels[0].Show();
				}
            }
		}

        /*if (labels.Count <= 0) 
		{
			create_label("gpu_hotspot", (double)dict["gpu_hotspot"],f1);
		}*/

        /*sb.Append("Cpu max temp: ");
        sb.Append((double)dict["cpu_core_max"]);
		cpu_temp_label.Text = sb.ToString();
		sb.Clear();*/
		if(labels.Count()<=0)
		{
			return;
		}

        
        for (int a=0;a<labels.Count;a++)
		{
            
            labels[a].num_val = Math.Round((double)dict[labels[a].key], 1);
            labels[a].Text = owner.loc_names[labels[a].key] + labels[a].num_val;


			

			bool unix_time = false;

			try
			{
				unix_time = (bool)dict["boot_time_unix"];

            }
			catch
			{

			}

            //Console.WriteLine("Using unix time?");
            //Console.WriteLine(unix_time);

			if(unix_time)
			{
				DateTime sent_t = new();
                sent_t= sent_t.AddSeconds((long)dict["sent_time"]).ToLocalTime();

                DateTime boot_t = new();
                boot_t = boot_t.AddSeconds((long)dict["boot_time"]).ToLocalTime();

				Console.WriteLine(sent_t);
                Console.WriteLine(boot_t);

				

                try
				{
					timestamp_label.Text = sent_t.ToString("HH:mm:ss");
					timestamp_label.Show();
				}
				catch { }

                var time_diff = (sent_t - boot_t).Duration();
				
				StringBuilder st = new StringBuilder();
                st.Append(time_diff);

                if (time_diff.Days>0)
				{
                   
                    st[1] = 'd';
                    st.Insert(2, ' ');
                }

				else
				{
                    //st.Remove(0,2);
                }
				
                if (runtime_label.Text.Length!=st.Length)
				{
					runtime_label.Left -= (st.Length - runtime_label.Text.Length) * 8; 
				}
				runtime_label.Text = st.ToString();

            }


            if (!unix_time && has_boot_time != null && has_boot_time == true)
            {

                try
                {
                    timestamp_label.Text = (string)dict["sent_time"];
                    timestamp_label.Show();
                }
                catch { }


                var sent_t =DateTime.ParseExact((string)dict["sent_time"], "HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);


				var boot_t = DateTime.ParseExact((string)dict["boot_time"], "HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                
				
				var time_diff = sent_t - boot_t;
				var st = time_diff.ToString();
                //Console.WriteLine(Convert.ToString(boot_t));
                if (runtime_label.Text.Length != st.Length)
                {
                    runtime_label.Left -= (st.Length - runtime_label.Text.Length) * 8;
                }

                runtime_label.Text = st;
            }



        }

        Console.WriteLine("Number of labels");
		Console.WriteLine(labels.Count());
        Console.WriteLine("Number of ranges");
        Console.WriteLine(owner.temp_ranges[labels[slide_page].key].Count());



		update_color();
        




    }

	void chance_text_color(Color c)
	{
		comp_name_label.ForeColor = c;
		timestamp_label.ForeColor = c;
		foreach(Custom_label lb in labels)
		{
			lb.ForeColor = c;
		}

    }

	void chance_back_color(Color c) 
	{ 
	     this.BackColor = c;
		 next_btn.FlatAppearance.MouseOverBackColor = c;
        


    }

	public void blink()
	{
		chance_text_color(Color.White);


		if(this.BackColor != Color.DarkRed && this.BackColor != Color.White)
		{
			if(slide_page < labels.Count)
			{
                Extreme_temp_report ex = new(get_name(), labels[slide_page].num_val, DateTime.Now, labels[slide_page].Text, 1.0);
            }
            
        }

        if (owner == null||labels.Count <=0) { return; }
        if (labels[slide_page].num_val > owner.temp_ranges[labels[slide_page].key][2]) 
		{
		       if(this.BackColor == Color.DarkRed)
			   {
				  chance_back_color(Color.White);
				  chance_text_color(Color.Black);

               }
			   else 
			   {
                chance_back_color(Color.DarkRed);
                 chance_text_color(Color.White);
               }
		}
	}


	void repos(Point p)
	{
		Location = p;
	}

	public void call_repos(Point p)
	{
        if (this.InvokeRequired)
        {
            var d = new pos_del(repos);
            this.Invoke(d,p);
        }
        else
        {
            repos(p);
        }
    }

	public void create_label(string key,double val,Form1 owner)
	{
		Custom_label lab = new Custom_label();


        
        lab.ForeColor = Color.White;
        lab.Font = LargeFont;
        lab.Location = new Point(0, 70);
        lab.AutoSize = true;
		lab.key = key;
		lab.num_val = val;


        //lab.Text = owner.loc_names[key] + val.ToString();


        labels.Add(lab);
		this.Controls.Add(lab);
		lab.Hide();
		lab.Click += next_page;
	}



	public void next_page()
	{
		//Console.WriteLine("Chance page");
		if(get_name() == def_name || labels.Count<2)
			{slide_page=0; return; }

		else
		{
			if(slide_page+1< labels.Count)
			{
				labels[slide_page].Hide();
				slide_page = +1;
                labels[slide_page].Show();

            }

			else
			{
                labels[slide_page].Hide();
                slide_page = 0;
                labels[slide_page].Show();
            }
		}
	}

    public void next_page(object sender, EventArgs e)
    {
        //Console.WriteLine("Chance page");
        if (get_name() == def_name || labels.Count < 2)
        { slide_page = 0; return; }

        else
        {
            if (slide_page + 1 < labels.Count)
            {
                labels[slide_page].Hide();
                slide_page = +1;
                update_color();
                labels[slide_page].Show();

            }

            else
            {
                labels[slide_page].Hide();
                slide_page = 0;
                update_color();
                labels[slide_page].Show();
            }

            
        }
    }

    public void prev_page()
    {
        //Console.WriteLine("Chance page");
        if (get_name() == def_name || labels.Count < 2)
        { slide_page = 0; return; }

        else
        {
            if (slide_page - 1 >= 0)
            {
                labels[slide_page].Hide();
                slide_page = +1;
                labels[slide_page].Show();

            }

            else
            {
                labels[slide_page].Hide();
                slide_page = labels.Count-1;
                labels[slide_page].Show();
            }

			update_color();
        }
    }


	public void set_page(int i) 
	{
		if (!has_computer  || i >= labels.Count) { return; }
        labels[slide_page].Hide();
        slide_page = i;
        update_color();
        labels[slide_page].Show();
		
        
	}


	public void reset_beep(object sender, EventArgs e)
	{
		can_beep = true;
	}
	public void send_beep(object sender, EventArgs e)
	{

		if (!can_beep) { return; }
        byte[] msg = Encoding.ASCII.GetBytes("Beep");
		socket.SendAsync(msg);
		can_beep = false;

    }

	
}
