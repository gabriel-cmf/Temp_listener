namespace Temp_listener_forms;
using System.Threading;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    /// 

    static void run_listener()
    {
        
        Console.WriteLine("Running listener");
        Listener lst = new Listener();
        Console.WriteLine(Form.ActiveForm.GetType());
        lst.main_loop2((Form1)Form.ActiveForm);
       
    }


    [STAThread]
    static void Main()
    {

        //Console.Beep(1000,1000);
        //Listener lst = new Listener();
        //var mf = new Form1();
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        // Console.WriteLine(Application.OpenForms[0].GetType());
       


        Thread t = new Thread(run_listener) { IsBackground = true };

        Application.Idle += delegate 
        {
            if (!t.IsAlive) {Console.WriteLine("Starting thread"); t.Start(); }
        };
        
        Application.Run(new Form1());
        

       



        

        
    }    
}