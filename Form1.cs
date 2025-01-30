using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Drawing;
using CsvHelper.Configuration.Attributes;
using CsvHelper;
using System.Globalization;
using System.Text;
using static System.Windows.Forms.DataFormats;
using System.DirectoryServices;
using System.IO;


namespace Temp_listener_forms;


struct string_pair
{
    [Name("key")]
    public string key { get; set; }
    [Name("label_name")]
    public string val { get; set; }
}

struct temp_val
{
    [Name("key")]
    public string key { get; set; }
    [Name("medium")]
    public int med { get; set; }
    [Name("high")]
    public int high { get; set; }
    [Name("extreme")]
    public int ext { get; set; }
}


public partial class Form1 : Form
{
    public static string titulo = "Monitor de temperatura";
    public List<Custom_panel> panels;
    public int grid_rows;
    public int grid_cols;
    public bool closed;
    public Dictionary<string, string> loc_names;
    public Dictionary<string, int[]> temp_ranges;
    public System.Windows.Forms.Timer timer;
    public System.Windows.Forms.Timer page_timer;
    public System.Windows.Forms.Timer title_timer;
    public List<string> allowed_keys;
    public List<string> pc_blacklist;
    public bool keys_set;
    int internal_page;
    private delegate void SafeCallDelegateVoid(string s);
    private delegate void SafeCallDelegateVoid2(Custom_panel p);
    private delegate void SafeCallDelegateInt(int a);
    public string log_file;
    public DateTime last_date;

    public Form1()
    {
        grid_rows = 4;
        grid_cols = 5;
        internal_page = 0;
        panels = new();
        this.WindowState = FormWindowState.Maximized;
        MaximizeBox = true;

        var screen_size = Screen.GetWorkingArea(this);

        //MinimizeBox = false;

        int panel_w = screen_size.Width / grid_cols;
        int panel_h = ((screen_size.Height) / grid_rows) - (20 / grid_rows);
        timer = new();
        timer.Interval = 500;
        timer.Tick += do_flash;

        keys_set = false;

        page_timer = new();
        page_timer.Interval = 10000;
        page_timer.Tick += panel_move_page;


        title_timer = new();
        title_timer.Interval = 500;
        title_timer.Tick += change_title;

        loc_names = new();
        read_loc();

        temp_ranges = new();
        read_temp_ranges();

        allowed_keys = new();
        read_allowed_keys();

        pc_blacklist = new List<string>();
        read_pc_blacklist();



        Console.WriteLine("Allowed keys count");
        Console.WriteLine(allowed_keys.Count());



        for (int a = 0; a < grid_rows; a++)
        {
            for (int b = 0; b < grid_cols; b++)
            {
                Custom_panel p = new Custom_panel(this);
                //p.Width = panel_w; p.Height = panel_h;
                //p.ClientSize = new(panel_w,panel_h);
                p.Size = new Size(panel_w, panel_h);
                p.Location = new Point(b * panel_w, a * panel_h);
                p.align();
                this.Controls.Add(p);
                panels.Add(p);
            }
        }

        closed = false;

        this.FormClosing += delegate { Console.WriteLine("Closing"); closed = true; };



        if (allowed_keys.Any()) { preload_keys(); }






        //Console.WriteLine(Screen.GetWorkingArea(this));
        InitializeComponent();

        this.Text = titulo;

        timer.Start();
        page_timer.Start();
        title_timer.Start();

        if (!Directory.Exists("logs"))
        {
            // Create the folder
            Directory.CreateDirectory("logs");
            Console.WriteLine("Folder created successfully.");
        }


        log_file = "logs/log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
        if (!File.Exists(log_file))
        {
            using (FileStream fs = new FileStream(log_file, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.WriteLine("Computador,Data,Sensor,valor");
                }
            }




            //File.AppendAllText(log_file, "Computador,Data,Sensor,valor");
            last_date = DateTime.Now;


        }

        //this.FormBorderStyle = FormBorderStyle.None;


        //this.Controls.Add(abt);



    }

    public void read_loc()
    {
        Console.WriteLine("Read loc");
        using (var reader = new StreamReader("config/localization.txt"))
        {
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<string_pair>();
                //Console.WriteLine("records size");
                //Console.WriteLine(records.Count());
                //Console.WriteLine(records.First());
                //Console.WriteLine(records.ToHashSet().Count());
                //Console.WriteLine(records.ToList().Count());

                //List<pos_entry> l_r  = records.ToList();


                foreach (var p in records)
                {
                    //Console.WriteLine(p.val);
                    loc_names[p.key] = p.val;
                }
            }
        }

        //Console.WriteLine(loc_names["gpu_hotspot"]);
    }

    public void read_temp_ranges()
    {
        Console.WriteLine("Read temp ranges");
        using (var reader = new StreamReader("config/value_ranges.txt"))
        {
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<temp_val>();
                //Console.WriteLine("records size");
                //Console.WriteLine(records.Count());
                //Console.WriteLine(records.First());
                //Console.WriteLine(records.ToHashSet().Count());
                //Console.WriteLine(records.ToList().Count());

                //List<pos_entry> l_r  = records.ToList();


                foreach (var p in records)
                {
                    Console.WriteLine(p.key);
                    temp_ranges[p.key] = new int[3];
                    temp_ranges[p.key][0] = p.med;
                    temp_ranges[p.key][1] = p.high;
                    temp_ranges[p.key][2] = p.ext;
                }
            }
        }

        //Console.WriteLine(loc_names["gpu_hotspot"]);
    }

    public void do_flash(object? sender, EventArgs e)
    {

        if (last_date.DayOfYear != DateTime.Now.DayOfYear)
        {
            Console.WriteLine("Date file update");
            last_date = DateTime.Now;

            log_file = "logs/log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
            if (!File.Exists(log_file))
            {
                using (FileStream fs = new FileStream(log_file, FileMode.Create))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.WriteLine("Computador,Data,Sensor,valor");
                    }
                }




                //File.AppendAllText(log_file, "Computador,Data,Sensor,valor");
            }
        }



        for (int a = 0; a < panels.Count; a++)
        {
            panels[a].blink();
        }

    }


    public void panel_move_page(object? sender, EventArgs e)
    {
        Console.WriteLine("Next page");
        if (keys_set == true)
        {
            internal_page += 1;
            if (internal_page >= allowed_keys.Count)
            {
                internal_page = 0;

            }

            for (int a = 0; a < panels.Count; a++)
            {
                panels[a].set_page(internal_page);
            }
        }
        else
        {
            for (int a = 0; a < panels.Count; a++)
            {
                panels[a].next_page();
            }
        }
    }

    public int get_temp_med(string val)
    {
        return temp_ranges[val][0];
    }
    public int get_temp_high(string val)
    {
        return temp_ranges[val][1];
    }

    public int get_temp_ext(string val)
    {
        return temp_ranges[val][2];
    }


    public void read_allowed_keys()
    {
        const Int32 BufferSize = 512;
        if (File.Exists("config/allowed_keys.txt"))
        {
            using (var fileStream = File.OpenRead("config/allowed_keys.txt"))
            {
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                {
                    string line;

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        allowed_keys.Add(line);
                    }
                }
            }
        }
    }


    public void read_pc_blacklist()
    {

        const Int32 BufferSize = 512;
        if (File.Exists("config/computer_blacklist.txt"))
        {
            using (var fileStream = File.OpenRead("config/computer_blacklist.txt"))
            {
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                {
                    string line;

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        //Console.WriteLine(line);
                        pc_blacklist.Add(line);
                    }
                }
            }
        }

    }


    public bool is_key_allowed(string k)
    {
        if (allowed_keys.Count <= 0) { return true; }



        return allowed_keys.Contains(k);

    }

    public void preload_keys()
    {
        for (int i = 0; i < allowed_keys.Count; i++)
        {
            for (int j = 0; j < panels.Count; j++)
            {
                panels[j].create_label(allowed_keys[i], 0, this);
                panels[j].used_keys.Add(allowed_keys[i]);
                if (i == 0) { panels[j].labels[i].Show(); }
            }
        }

        Console.WriteLine(panels[0].labels.Count);
        keys_set = true;

    }

    public void call_set_title(string t)
    {
        if (this.InvokeRequired)
        {
            var d = new SafeCallDelegateVoid(set_title);
            this.Invoke(d, t);
        }
        else
        {
            set_title(t);
        }
    }
    void set_title(string t)
    {
        Text = t;
    }

    void change_title(object? sender, EventArgs e)
    {
        if (Text == "Monitor de temperatura Ooo")
        {
            // mf.Text = "Monitor de temperatura oOo";
            call_set_title("Monitor de temperatura oOo");
            return;
        }


        if (Text == Form1.titulo || Text == "Monitor de temperatura ooO")
        {
            //mf.Text = "Monitor de temperatura Ooo";
            call_set_title("Monitor de temperatura Ooo");
            return;
        }

        if (Text == "Monitor de temperatura oOo")
        {
            //mf.Text = "Monitor de temperatura ooO";
            call_set_title("Monitor de temperatura ooO");
            return;
        }
    }


    public void call_remove_panels(Custom_panel p)
    {
        if (this.InvokeRequired)
        {
            var d = new SafeCallDelegateVoid2(remove_panel);
            this.Invoke(d, p);
        }
        else
        {
            remove_panel(p);
        }
    }

    public void call_add_panel(Custom_panel p)
    {
        if (this.InvokeRequired)
        {
            var d = new SafeCallDelegateVoid2(add_panel);
            this.Invoke(d, p);
        }
        else
        {
            add_panel(p);
        }
    }
    void remove_panel(Custom_panel p)
    {
        this.Controls.Remove(p);
    }

    void add_panel(Custom_panel p)
    {
        this.Controls.Add(p);
    }

    public void rebuild_panels()
    {

        var screen_size = Screen.GetWorkingArea(this);
        int panel_w = screen_size.Width / grid_cols;
        int panel_h = screen_size.Height / grid_rows;
        for (int a = 0; a < grid_rows; a++)
        {
            for (int b = 0; b < grid_cols; b++)
            {
                Custom_panel p = new Custom_panel(this);
                //p.Width = panel_w; p.Height = panel_h;
                //p.ClientSize = new(panel_w,panel_h);
                p.Size = new Size(panel_w, panel_h);
                p.Location = new Point(b * panel_w, a * panel_h);
                p.align();
                this.Controls.Add(p);
                panels.Add(p);
            }
        }
    }

    void create_panel_at(int a)
    {

        if (a < 0 || a >= panels.Count)
        {
            Console.WriteLine("Invalid pos on create panel");
            return;
        }

        if (panels[a] != null)
        {
            //var pl = panels[a];
            panels[a].Dispose();
            //pl.Dispose();
        }

        var screen_size = Screen.GetWorkingArea(this);
        int panel_w = screen_size.Width / grid_cols;
        int panel_h = screen_size.Height / grid_rows;
        int b = a / grid_rows;


        Custom_panel p = new Custom_panel(this);
        //p.Width = panel_w; p.Height = panel_h;
        //p.ClientSize = new(panel_w,panel_h);
        p.Size = new Size(panel_w, panel_h);
        p.Location = new Point(b * panel_w, a * panel_h);
        p.align();
        this.Controls.Add(p);
        panels[a] = p;
    }

    public void call_create_panel_at(int a)
    {
        if (this.InvokeRequired)
        {
            var d = new SafeCallDelegateInt(create_panel_at);
            this.Invoke(d, a);
        }

        else
        {
            create_panel_at(a);
        }
    }

    public void write_event(Extreme_temp_report ex)
    {

        File.AppendAllText(log_file, ex.get_string() + Environment.NewLine);

    }

    private void Form1_Load(object sender, EventArgs e)
    {
        AboutBox1 a = new AboutBox1();
        a.Show();
    }
}
