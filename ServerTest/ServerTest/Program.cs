using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SimEngineLibrary;
using SimioAPI;
using SimEngineInterfaceHelpers;
using System.IO;
using System.Globalization;
using CsvHelper;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Random;



namespace ServerTest
{
    #region New

    class Program
    {
        static void Main(string[] args)
        {
            //Console.ReadKey();

            Thread serverThread = new Thread(() => new Server("192.168.1.128", 11000)); //"172.24.1.218" 172.24.1.218
            serverThread.Start();

            // server is going to be launch from here
            
            Console.WriteLine("Server Started.");
        }
    }

    public class csvStructure
    {
        public int ExpressionProperty1 { get; set; }
    }

    public class Server
    {
        private static object _lock = new object();

        private readonly TcpListener server;

        private int clientCount = 1;

        private static Dictionary<int, TcpClient> clients = new Dictionary<int, TcpClient>();

        ISimioProject project;

        static MersenneTwister random = new MersenneTwister(1); // seed 1
        static Exponential expo = new Exponential(0.333333, random);



        public Server(string ip, int port)
        {
            IPAddress localAddress = IPAddress.Parse(ip);
            server = new TcpListener(localAddress, port);


            server.Start();
            StartListener();
        }

        public void StartListener()
        {
            string extensionsPath = "C:\\Users\\ak574\\source\\repos\\ServerTest\\ServerTest\\bin\\Debug";
            string projectPath = "C:\\Users\\ak574\\Documents\\RoverDESv4.spfx";
            string saveProjectPath = "C:\\Users\\ak574\\Documents\\RoverDESv4.spfx";
            string explanation = "";
            SortedDictionary<int, double[]> resultsDict = new SortedDictionary<int, double[]>();


            Console.WriteLine("Loading Simio...");

            try
            {

                project = SimEngineHelpers.LoadProject(extensionsPath, projectPath, out explanation);
            }
            catch (Exception ex)
            {

                throw;
            }

            //Console.WriteLine("Simio Loaded. Waiting for input...");
            //Console.ReadKey();

            //Console.WriteLine("Input received. Running experiment...");

            //RunModelExperiment(project, 5, 1, 0, 4, 10, out resultsDict);
            //double[] responseRow2 = SelectResponse_BP(resultsDict, 20);
            //Console.WriteLine($"Cars to use: {responseRow2[2]}. Speed: {responseRow2[3]}. BPEnd: {responseRow2[0]}");

            //Console.ReadKey();

            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for client connections...");
                    TcpClient client = server.AcceptTcpClient();

                    lock (_lock)
                    {
                        clients.Add(clientCount, client);
                    }

                    Console.WriteLine("ClientId {clientCount} is connected and added to our clients...");
                    Thread thread = new Thread(HandleClientConnection);
                    thread.Start(clientCount);

                    clientCount++;
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"SocketException: {e}");
                server.Stop();
            }
        }

        public void HandleClientConnection(Object obj)
        {
            int clientId = (int)obj;
            string modelName = "";
            string experimentName = "";
            string scenarioResults = "";
            string explanation = "";
            SortedDictionary<int, double[]> resultsDict = new SortedDictionary<int, double[]>();
            //DictResults = null;



            TcpClient client = null;

            lock (_lock)
            {
                client = clients[clientId];
            }

            while (true)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int byteCount = stream.Read(buffer, 0, buffer.Length);

                if (byteCount == 0)
                {
                    break;
                }

                string data = Encoding.ASCII.GetString(buffer, 0, byteCount);

                if (data.Contains('@'))
                {
                    //This is an end-of-run data download.
                    string[] dataSent = data.Split('@');
                    foreach (string item in dataSent)
                    {
                        Console.WriteLine(item);
                    }
                }
                else
                {
                    //This is a simio request.
                    #region simioRequest

                    Console.WriteLine("Data received...");
                    Console.WriteLine(data);
                    string[] dataSent = data.Split('#');
                    double observedMin = Convert.ToDouble(dataSent[0]);
                    double observedMax = Convert.ToDouble(dataSent[1]);
                    int ballsWaiting = Convert.ToInt16(dataSent[2]);
                    Console.WriteLine($"Current min arrival: {observedMin}. Current max arrival: {observedMax}. Balls waiting: {ballsWaiting}");

                    Console.WriteLine("Running first experiment...");

                    //Determine number of reps.
                    int reps = 5;

                    //Set stage
                    int stage = 1;

                    //Run the experiment.
                    RunModelExperiment(project, reps, stage, observedMin, observedMax, ballsWaiting, out resultsDict);

                    Console.WriteLine("First experiment complete. Making decision...");
                    double[] responseRow = SelectResponse_BP(resultsDict, 20);
                    Console.WriteLine("Decision complete. Results below.");
                    Console.WriteLine($"Cars to use: {responseRow[2]}. Speed: {responseRow[3]}. BPEnd: {responseRow[0]}");
                    string response = ($"Stage{stage}#{responseRow[0]:f2}#{responseRow[1]:f2}#{responseRow[2]}#{responseRow[3]}#{responseRow[4]}");


                    Console.WriteLine(response);

                    Broacast(response);

                    Console.WriteLine("Running second experiment...");

                    //Determine number of reps.
                    reps = 1;

                    //Set stage
                    stage = 2;

                    //Run the experiment.
                    RunModelExperiment(project, reps, stage, observedMin, observedMax, 0, out resultsDict);

                    Console.WriteLine("Second experiment complete. Making decision...");
                    responseRow = SelectResponse_SteadyState(resultsDict, 15);
                    Console.WriteLine("Decision complete. Results below.");
                    Console.WriteLine($"Cars to use: {responseRow[2]}. Speed: {responseRow[3]}. Recharge: {responseRow[4]}");
                    response = ($"Stage{stage}#{responseRow[0]}#{responseRow[1]}#{responseRow[2]}#{responseRow[3]}#{responseRow[4]}");


                    Console.WriteLine(response);

                    Broacast(response);

                    #endregion
                }


            }

            lock (_lock)
            {
                clients.Remove(clientId);
            }

            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        public static void Broacast(string message)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message + Environment.NewLine);

            lock (_lock)
            {
                foreach (TcpClient client in clients.Values)
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }

        private double[] SelectResponse_BP(SortedDictionary<int, double[]> resultsDict, double threshold)
        {
            foreach (KeyValuePair<int,double[]> result in resultsDict)
            {
                //BusyPeriodEnd is located at the first index of the array.
                if (result.Value[0] < threshold && result.Value[0] != 0)
                {
                    return result.Value;
                }
            }

            //If we reach this part of the code, no scenarios met our threshold. Return the max of our controls.
            return new double[] { 0, 0, 5, 2, 20 }; //Car, speed, recharge
        }

        private double[] SelectResponse_SteadyState(SortedDictionary<int, double[]> resultsDict, double threshold)
        {
            foreach (KeyValuePair<int, double[]> result in resultsDict)
            {
                //AvgTIS is located at the second index of the array.
                if (result.Value[1] < threshold)
                {
                    return result.Value;
                }
            }

            //If we reach this part of the code, no scenarios met our threshold. Return the max of our controls.
            return new double[] { 0, 0, 5, 2, 20 }; //Car, speed, recharge
        }

        #region Simio Calls
        public static bool RunModelExperiment(ISimioProject project, int numReps, int stage, double observedMin, double observedMax, int initialQty, out SortedDictionary<int, double[]> DictResults)
        {
            //ISimioProject project = null;
            string explanation = "";
            string marker = "Begin";
            string modelName = "";
            string experimentName = "";
            string scenarioResults = "";
            SortedDictionary<int, double[]> resultsDict = new SortedDictionary<int, double[]>();
            DictResults = null;

            //SimioProjectFactory.SetExtensionsPath(Directory.GetCurrentDirectory().ToString());
            string extensionsPath = "C:\\Users\\ak574\\source\\repos\\ServerTest\\ServerTest\\bin\\Debug";
            string projectPath = "C:\\Users\\ak574\\Documents\\RoverDESv4.spfx";
            string saveProjectPath = "C:\\Users\\ak574\\Documents\\RoverDESv4.spfx";

            try
            {

                //project = SimEngineHelpers.LoadProject(extensionsPath, projectPath, out explanation);
                if (project == null)
                {
                    explanation = $"Cannot load project={projectPath}. Reason={explanation}";
                    //return false;
                }


                //foreach (var model in project.Models)
                //{
                //    Console.WriteLine(model.Name);
                //}

                IModel defaultModel = project.Models[1];
                modelName = defaultModel.Name; 
                //comboHeadlessRunModels.Enabled = true;

                

                foreach (var experiment in defaultModel.Experiments)
                {
                    experimentName = experiment.Name;
                }

                Console.WriteLine(" Using model: " + modelName);
                Console.WriteLine(" Using experiment: " + experimentName);


                //return true;
            }
            catch (Exception ex)
            {
                explanation = $"Project={projectPath} Err={ex.Message}";
                //return false;
            }
            finally
            {
                //Cursor.Current = Cursors.Default;
            }


            try
            {
                marker = $"Loading Model named={modelName}";
                IModel model = LoadModel(project, modelName, out explanation);
                if (model == null)
                    return false;

                //model.ImportDataConnectors();
                marker = $"Loading Experiment named={experimentName}";
                IExperiment experiment = LoadExperiment(model, experimentName, out explanation);
                if (experiment == null)
                    return false;

                // Create some methods to handle experiment events. Events are:
                // RunStarted, RunProgessChanged, RunCompleted
                // ScenarioStarted, ScenarioEnded
                // ReplicationStarted, ReplicationEnded
                // 
                ExperimentResults experimentResults = new ExperimentResults();

                System.Text.StringBuilder myResults = new System.Text.StringBuilder();

                // Here the 'e' is RunStartedEventArgs
                experiment.RunStarted += (s, e) =>
                {
                    Console.WriteLine($"Info: Event=> Experiment={experiment.Name} Run Started. ");
                };

                // Here the 'e' is ReplicationEndedEventArgs
                experiment.ReplicationEnded += (s, e) =>
                {
                    Console.WriteLine($"Info: Event=> Ended Replication={e.ReplicationNumber} of Scenario={e.Scenario.Name}.");
                };

                // Here the 'e' is ScenarioStartedEventArgs
                experiment.ScenarioStarted += (s, e) =>
                {
                    Console.WriteLine($"Info: Event=> Scenario={e.Scenario.Name} Started.");

                };
                experiment.ScenarioEnded += (s, e) =>
                {
                    Console.WriteLine($"Info: Event=> Scenario={e.Scenario.Name} Ended.");

                    //Console.WriteLine($"ScenarioEnded: Results:");

                    //foreach (var result in e.Results) 
                    //{
                    //    // Log the results to get a feel for what is being returned.
                    //    Console.WriteLine($"ScenarioEnded: ObjType='{result.ObjectType}', ObjName='{result.ObjectName}', DS='{result.DataSource}', " +
                    //        $"Cat='{result.StatisticCategory}', DataItem='{result.DataItem}', Stat='{result.StatisticCategory}', " +
                    //        $"Avg='{result.Average:0.00}', Min='{result.Minimum:f0.00}', Max='{result.Maximum:f0.00}");

                    //    if (result.ObjectType == "ModelEntity" && result.ObjectName == "DefaultEntity" && result.DataSource == "[Population]" && result.StatisticCategory == 
                    //        "FlowTime" && result.DataItem == "TimeInSystem")
                    //    {

                    //    }

                    //    experimentResults.AddScenarioResults(e.Scenario, e.Results);
                    //}
                };

                //scenarioResults = myResults.ToString();

                // Here the 'e' is RunCompletedEventArgs
                experiment.RunCompleted += (s, e) =>
                {
                    Console.WriteLine($"Info: Event=> Experiment={experiment.Name} Run Complete. ");
                    foreach (ITable table in model.Tables)
                    {
                        table.ExportForInteractive();
                    }

                };

                // Now do the run.

                experiment.Reset();

                //string experimentControl = experiment.Controls[0].Name;
                IExperimentControl experimentControl = experiment.Controls[0];// project.Models[0].Experiments[0].Controls[0];

                
                int i = 0;
                int rovers = 1;
                int roverLvls = 5;
                double speed = 1;
                int speedLvls = 6;
                int rechargeLevel = 20;
                int rechargeLevelLvls = 5;
                int numScenarios = roverLvls * speedLvls * rechargeLevelLvls;

                foreach (var scenario in experiment.Scenarios)
                {
                    //Set the constant controls here.
                    experiment.Scenarios[i].Active = true;
                    experiment.Scenarios[i].ReplicationsRequired = numReps;
                    experiment.Scenarios[i].ControlValues["ArrivalsMin"] = observedMin.ToString();
                    experiment.Scenarios[i].ControlValues["ArrivalsMax"] = observedMax.ToString();
                    experiment.Scenarios[i].ControlValues["InitialBallsInSys"] = initialQty.ToString();


                    //Let these controls vary.
                    if (stage == 1)
                    {
                        //Set recharge level to constant.
                        rechargeLevel = 30;
                        numScenarios = roverLvls * speedLvls;
                    }

                    if (i != 0 && i % roverLvls == 0)
                    {
                        //We've reached the top level of rovers. Reset rovers and increment speed.
                        rovers = 1;
                        speed += 0.2;
                    }
                    if (speed > 2)
                    {
                        if (stage == 1)
                        {
                            //We've reached the top level of speed. End scenario build.
                            Console.WriteLine($"End of stage {stage} scenario generation. Using {i} scenarios of {experiment.Scenarios.Count} available.");
                            break;
                        }
                        else if (stage == 2)
                        {
                            speed = 1;
                            rechargeLevel += 10;

                            if (rechargeLevel > 60)
                            {
                                //We've reached the top level of rechargeLevel. End scenario build.
                                Console.WriteLine($"End of stage {stage} scenario generation. Using {i} scenarios of {experiment.Scenarios.Count} available.");
                                break;
                            }
                        }
                            
                    }

                    //if (i > roverLvls * speedLvls)
                    //{
                    //    experiment.Scenarios[i].Active = false;
                    //}
                    //else
                    //{
                    //    experiment.Scenarios[i].Active = true;
                    //}

                    experiment.Scenarios[i].ControlValues["RoverSpeed"] = speed.ToString();
                    experiment.Scenarios[i].ControlValues["NumRovers"] = rovers.ToString();
                    experiment.Scenarios[i].ControlValues["LowChargeLevel"] = rechargeLevel.ToString();
                    rovers++;
                    i++;
                }

                experiment.Run();

                i = 0;
                double[] resultsArray;

                foreach (var scenario in experiment.Scenarios)
                {
                    var responseValues_BPEnd = experiment.Scenarios[i].ResponseValues["BPEnd"];
                    ////Remove zero values (this means simio did not see a scenario where BP cleared).
                    //if (responseValues_BPEnd == 0)
                    //{
                    //    responseValues_BPEnd += 10000;
                    //}
                    var responseValues_AvgTIS = experiment.Scenarios[i].ResponseValues["AvgTIS"];
                    var controlValues_rovers = experiment.Scenarios[i].ControlValues["NumRovers"];
                    var controlValues_speed = experiment.Scenarios[i].ControlValues["RoverSpeed"];
                    var controlValues_chargeLvl = experiment.Scenarios[i].ControlValues["LowChargeLevel"];
                    var responseValues_MaxNIQ = experiment.Scenarios[i].ResponseValues["MaxNIQ"];

                    resultsDict.Add(i, new double[] { responseValues_BPEnd, responseValues_AvgTIS, Convert.ToDouble(controlValues_rovers), Convert.ToDouble(controlValues_speed), Convert.ToDouble(controlValues_chargeLvl), responseValues_MaxNIQ });
                    resultsArray = resultsDict[i];

                    myResults.Append($"Rovers: {resultsArray[2]}. Speed: {resultsArray[3]}. ChargeLevel: {resultsArray[4]}. BPEnd: {resultsArray[0]}. AvgTIS: {resultsArray[1]}. MaxNIQ: {resultsArray[5]}");
                    myResults.Append("\n");
                    i++;
                    if (i == numScenarios)
                    {
                        break;
                    }
                }
                

                //experiment.Run();

                //experiment.RunAsync(); // Another option
                //SimEngineLibrary.ExperimentResults();
                // Let's look at some results
                //var response = experiment.Responses[0].Experiment.Scenarios[0].ResponseValues;


                //i = 0;

                //foreach (var scenario in experiment.Scenarios)
                //{
                //    var responseValues = experiment.Scenarios[i].ResponseValues["AvgTIS"];
                //    var controlValues = experiment.Scenarios[i].ControlValues["NumRovers"];

                //    resultsDict.Add(Int16.Parse(controlValues), responseValues);

                //    myResults.Append($"Rovers: {controlValues}. Response: {responseValues}");
                //    myResults.Append("\n");
                //    i++;
                //}
                //IExperimentResponse response1 = response;
               
                scenarioResults = myResults.ToString();
                DictResults = resultsDict;
                Console.WriteLine(scenarioResults);
                //Console.ReadKey();



                //foreach (var result in response.Average)
                //{
                //    // Log the results to get a feel for what is being returned.
                //    Console.WriteLine($"ScenarioEnded: ObjType='{result.ObjectType}', ObjName='{result.ObjectName}', DS='{result.DataSource}', " +
                //        $"Cat='{result.StatisticCategory}', DataItem='{result.DataItem}', Stat='{result.StatisticCategory}', " +
                //        $"Avg='{result.Average:0.00}', Min='{result.Minimum:f0.00}', Max='{result.Maximum:f0.00}");

                //    
                //}



                //if (File.Exists(saveProjectPath))
                //{
                //    marker = $"Save Project After Experiment Run to={saveProjectPath}";
                //    Console.WriteLine($"Info: {marker}");

                //    string[] warnings;
                //    SimioProjectFactory.SaveProject(project, saveProjectPath, out warnings);
                //    explanation = "";
                //    int nn = 0;
                //    foreach (string warning in warnings)
                //    {
                //        explanation += $"Save Warnings({warnings.Length}): Warning{++nn}:{warning}";
                //        Console.WriteLine($"Warning: {warning}");
                //    }
                //}

                return true;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Marker={marker} Err={ex.Message}");
            }
        }

        /// <summary>
        /// Load the model from the given project.
        /// Returns a Model object or a null if errors.
        /// </summary>
        /// <param name="projectFullPath"></param>
        private static IModel LoadModel(ISimioProject project, string modelName, out string explanation)
        {
            explanation = "";
            string marker = "Begin.";

            try
            {
                marker = $"Loading Model={modelName}";
                Console.WriteLine($"Info: {marker}");

                // Get the model from within the project
                var model = project.Models[modelName];
                if (model != null)
                {
                    if (model.Errors.Count == 0)
                    {
                        Console.WriteLine($"Info: Model={modelName} Successfully loaded.");
                    }
                    else
                    {
                        Console.WriteLine($"Model={modelName} not loaded due to {model.Errors.Count} errors. Individual errors follow:");
                        int errorCount = 0;
                        // Log any model errors
                        foreach (IError err in model.Errors)
                        {
                            Console.WriteLine($"  {++errorCount}. Error={err.ErrorText} Object={err.ObjectName} Type={err.ObjectType} Property: Name={err.PropertyName} Value={err.PropertyValue}");
                        }
                        return null;
                    }
                }
                else // model is null
                {
                    explanation = $"Model={modelName} could not be loaded from Project={project.Name}";
                    return null;
                }

                return model;
            }
            catch (Exception ex)
            {
                explanation = $"Cannot load={modelName} Err={ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// Load the experiment with name 'experimentName' from the given model.
        /// Returns an Experiment object or a null if errors.
        /// </summary>
        /// <param name="model">The model object; see LoadModel</param>
        /// <param name="experimentName">The name of an experiment with model</param>
        private static IExperiment LoadExperiment(IModel model, string experimentName, out string explanation)
        {
            explanation = "";
            string marker = "Begin.";

            if (model == null)
            {
                explanation = "Cannot load Experiment: Model is null.";
                return null;
            }

            try
            {
                marker = $"Loading Experiment={experimentName}";

                // Get the experiment from within the Model
                var experiment = model.Experiments[experimentName]; 
                if (experiment == null)
                {
                    explanation = $"Experiment={experimentName} Not Found within Model={model.Name}";
                    return null;
                }

                return experiment;
            }
            catch (Exception ex)
            {
                explanation = $"Cannot load={experimentName} Err={ex.Message}";
                return null;
            }
        }

        #endregion
    }

    #endregion

    #region Old
    // State object for reading client data asynchronously  
    //public class StateObject
    //{
    //    // Size of receive buffer.  
    //    public const int BufferSize = 1024;

    //    // Receive buffer.  
    //    public byte[] buffer = new byte[BufferSize];

    //    // Received data string.
    //    public StringBuilder sb = new StringBuilder();

    //    // Client socket.
    //    public Socket workSocket = null;

    //}

    //public class AsynchronousSocketListener
    //{


    //    // Thread signal.  
    //    public static ManualResetEvent allDone = new ManualResetEvent(false);

    //    public AsynchronousSocketListener()
    //    {
    //    }

    //    public static void StartListening()
    //    {
    //        // Establish the local endpoint for the socket.  
    //        // The DNS name of the computer  
    //        // running the listener is "host.contoso.com".  
    //        //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
    //        IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
    //        IPAddress ipAddress = ipHostInfo.AddressList[1];

    //        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

    //        // Create a TCP/IP socket.  
    //        Socket listener = new Socket(ipAddress.AddressFamily,
    //            SocketType.Stream, ProtocolType.Tcp);

    //        // Bind the socket to the local endpoint and listen for incoming connections.  
    //        try
    //        {
    //            listener.Bind(localEndPoint);
    //            listener.Listen(100);

    //            while (true)
    //            {
    //                // Set the event to nonsignaled state.  
    //                allDone.Reset();

    //                // Start an asynchronous socket to listen for connections.  
    //                Console.WriteLine("Waiting for a connection...");
    //                listener.BeginAccept(
    //                    new AsyncCallback(AcceptCallback),
    //                    listener);

    //                // Wait until a connection is made before continuing.  
    //                allDone.WaitOne();
    //            }

    //        }
    //        catch (Exception e)
    //        {
    //            Console.WriteLine(e.ToString());
    //        }

    //        Console.WriteLine("\nPress ENTER to continue...");
    //        Console.Read();

    //    }

    //    public static void AcceptCallback(IAsyncResult ar)
    //    {
    //        // Signal the main thread to continue.  
    //        allDone.Set();

    //        // Get the socket that handles the client request.  
    //        Socket listener = (Socket)ar.AsyncState;
    //        Socket handler = listener.EndAccept(ar);

    //        // Create the state object.  
    //        StateObject state = new StateObject();
    //        state.workSocket = handler;
    //        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
    //            new AsyncCallback(ReadCallback), state);
    //    }

    //    public static void ReadCallback(IAsyncResult ar)
    //    {
    //        String content = String.Empty;
    //        string modelName = "";
    //        string experimentName = "";

    //        // Retrieve the state object and the handler socket  
    //        // from the asynchronous state object.  
    //        StateObject state = (StateObject)ar.AsyncState;
    //        Socket handler = state.workSocket;

    //        // Read data from the client socket.
    //        int bytesRead = handler.EndReceive(ar);

    //        if (bytesRead > 0)
    //        {
    //            // There  might be more data, so store the data received so far.  
    //            state.sb.Append(Encoding.ASCII.GetString(
    //                state.buffer, 0, bytesRead));

    //            // Check for end-of-file tag. If it is not there, read
    //            // more data.  
    //            content = state.sb.ToString();
    //            if (content.IndexOf("<EOF>") > -1)
    //            {
    //                // All the data has been read from the
    //                // client. Display it on the console.  
    //                Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
    //                    content.Length, content);

    //                if (content == "Run Simio<EOF>")
    //                {
    //                    RunModelExperiment2(out modelName, out experimentName);
    //                }
    //                // Echo the data back to the client.  
    //                Send(handler, content);

    //                Send(handler, "Hello Quest. Simio run complete: Model ran: " + modelName + ". Experiment ran: " + experimentName + ".");

    //            }
    //            else
    //            {
    //                // Not all data received. Get more.  
    //                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
    //                new AsyncCallback(ReadCallback), state);
    //            }
    //        }
    //    }

    //    private static void Send(Socket handler, String data)
    //    {
    //        // Convert the string data to byte data using ASCII encoding.  
    //        byte[] byteData = Encoding.ASCII.GetBytes(data);

    //        // Begin sending the data to the remote device.  
    //        handler.BeginSend(byteData, 0, byteData.Length, 0,
    //            new AsyncCallback(SendCallback), handler);
    //    }

    //    private static void SendCallback(IAsyncResult ar)
    //    {
    //        try
    //        {
    //            // Retrieve the socket from the state object.  
    //            Socket handler = (Socket)ar.AsyncState;

    //            // Complete sending the data to the remote device.  
    //            int bytesSent = handler.EndSend(ar);
    //            Console.WriteLine("Sent {0} bytes to client.", bytesSent);

    //            handler.Shutdown(SocketShutdown.Both);
    //            handler.Close();

    //        }
    //        catch (Exception e)
    //        {
    //            Console.WriteLine(e.ToString());
    //        }
    //    }

    //    public static int Main(String[] args)
    //    {
    //        StartListening();
    //        return 0;
    //    }

    //    public static void LoadProject()
    //    {
    //        ISimioProject myProject;
    //        string extensionsPath = "C:\\Users\\ak574\\Downloads\\SimioApiHelper-master4\\SimioApiHelper-master\\SimEngineFormsExperiment\\bin\\Debug";
    //        string projectPath = "C:\\Users\\ak574\\Documents\\TestModel.spfx";
    //        string explanation = "";

    //        try
    //        {

    //            myProject = SimEngineHelpers.LoadProject(extensionsPath, projectPath, out explanation);
    //            if (myProject == null)
    //            {
    //                explanation = $"Cannot load project={projectPath}. Reason={explanation}";
    //                //return false;
    //            }


    //            foreach (var model in myProject.Models)
    //            {
    //                Console.WriteLine(model.Name);
    //            }

    //            IModel defaultModel = myProject.Models[1] as IModel;
    //            //comboHeadlessRunModels.Enabled = true;


    //            foreach (var experiment in defaultModel.Experiments)
    //            {
    //                Console.WriteLine(experiment.Name);
    //            }

    //            //return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            explanation = $"Project={projectPath} Err={ex.Message}";
    //            //return false;
    //        }
    //        finally
    //        {
    //            //Cursor.Current = Cursors.Default;
    //        }
    //    }

    //    /// <summary>
    //    /// Run an experiment. The experiment is Reset prior to run.
    //    /// If the saveProjectPath is present and exists, then the project will be
    //    /// saved to that location. If there are Save warnings, then true is still
    //    /// returned, but the warnings are in explanation.
    //    /// </summary>
    //    /// <param name="projectPathAndFile"></param>
    //    /// <param name="experimentName"></param>
    //    /// <param name="saveModelAfterRun"></param>
    //    /// <param name="explanation"></param>
    //    /// <returns></returns>
    //    public static bool RunModelExperiment2( out string modelName, out string experimentName)
    //    {
    //        ISimioProject project = null;
    //        string explanation = "";
    //        string marker = "Begin";
    //        modelName = "";
    //        experimentName = "";

    //        #region LoadProject

    //        string extensionsPath = "C:\\Users\\ak574\\Downloads\\SimioApiHelper-master4\\SimioApiHelper-master\\SimEngineFormsExperiment\\bin\\Debug";
    //        string projectPath = "C:\\Users\\ak574\\Documents\\TestModel.spfx";
    //        string saveProjectPath = "C:\\Users\\ak574\\Documents\\TestModel.spfx";

    //        try
    //        {

    //            project = SimEngineHelpers.LoadProject(extensionsPath, projectPath, out explanation);
    //            if (project == null)
    //            {
    //                explanation = $"Cannot load project={projectPath}. Reason={explanation}";
    //                //return false;
    //            }


    //            //foreach (var model in project.Models)
    //            //{
    //            //    Console.WriteLine(model.Name);
    //            //}

    //            IModel defaultModel = project.Models[1] as IModel;
    //            modelName = defaultModel.Name;
    //            //comboHeadlessRunModels.Enabled = true;


    //            foreach (var experiment in defaultModel.Experiments)
    //            {
    //                experimentName = experiment.Name;
    //            }

    //            Console.WriteLine(" Using model: " + modelName);
    //            Console.WriteLine(" Using experiment: " + experimentName);


    //            //return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            explanation = $"Project={projectPath} Err={ex.Message}";
    //            //return false;
    //        }
    //        finally
    //        {
    //            //Cursor.Current = Cursors.Default;
    //        }

    //        #endregion

    //        try
    //        {
    //            marker = $"Loading Model named={modelName}";
    //            IModel model = LoadModel(project, modelName, out explanation);
    //            if (model == null)
    //                return false;


    //            marker = $"Loading Experiment named={experimentName}";
    //            IExperiment experiment = LoadExperiment(model, experimentName, out explanation);
    //            if (experiment == null)
    //                return false;

    //            // Create some methods to handle experiment events. Events are:
    //            // RunStarted, RunProgessChanged, RunCompleted
    //            // ScenarioStarted, ScenarioEnded
    //            // ReplicationStarted, ReplicationEnded
    //            // 


    //            // Here the 'e' is RunStartedEventArgs
    //            experiment.RunStarted += (s, e) =>
    //            {
    //               Console.WriteLine($"Info: Event=> Experiment={experiment.Name} Run Started. ");
    //            };

    //            // Here the 'e' is ReplicationEndedEventArgs
    //            experiment.ReplicationEnded += (s, e) =>
    //            {
    //                Console.WriteLine($"Info: Event=> Ended Replication={e.ReplicationNumber} of Scenario={e.Scenario.Name}.");
    //            };

    //            // Here the 'e' is ScenarioStartedEventArgs
    //            experiment.ScenarioStarted += (s, e) =>
    //            {
    //                Console.WriteLine($"Info: Event=> Scenario={e.Scenario.Name} Started.");

    //            };
    //            experiment.ScenarioEnded += (s, e) =>
    //            {
    //                Console.WriteLine($"Info: Event=> Scenario={e.Scenario.Name} Ended.");
    //            };


    //            // Here the 'e' is RunCompletedEventArgs
    //            experiment.RunCompleted += (s, e) =>
    //            {
    //                Console.WriteLine($"Info: Event=> Experiment={experiment.Name} Run Complete. ");
    //                foreach (ITable table in model.Tables)
    //                {
    //                    table.ExportForInteractive();
    //                }

    //            };

    //            // Now do the run.
    //            experiment.Reset();
    //            experiment.Run();
    //            //experiment.RunAsync(); // Another option

    //            // Let's look at some results
    //            var response = experiment.Responses;



    //            if (File.Exists(saveProjectPath))
    //            {
    //                marker = $"Save Project After Experiment Run to={saveProjectPath}";
    //                Console.WriteLine($"Info: {marker}");

    //                string[] warnings;
    //                SimioProjectFactory.SaveProject(project, saveProjectPath, out warnings);
    //                explanation = "";
    //                int nn = 0;
    //                foreach (string warning in warnings)
    //                {
    //                    explanation += $"Save Warnings({warnings.Length}): Warning{++nn}:{warning}";
    //                    Console.WriteLine($"Warning: {warning}");
    //                }
    //            }

    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw new ApplicationException($"Marker={marker} Err={ex.Message}");
    //        }
    //    }

    //    /// <summary>
    //    /// Load the model from the given project.
    //    /// Returns a Model object or a null if errors.
    //    /// </summary>
    //    /// <param name="projectFullPath"></param>
    //    private static IModel LoadModel(ISimioProject project, string modelName, out string explanation)
    //    {
    //        explanation = "";
    //        string marker = "Begin.";

    //        try
    //        {
    //            marker = $"Loading Model={modelName}";
    //            Console.WriteLine($"Info: {marker}");

    //            // Get the model from within the project
    //            var model = project.Models[modelName];
    //            if (model != null)
    //            {
    //                if (model.Errors.Count == 0)
    //                {
    //                    Console.WriteLine($"Info: Model={modelName} Successfully loaded.");
    //                }
    //                else
    //                {
    //                    Console.WriteLine($"Model={modelName} not loaded due to {model.Errors.Count} errors. Individual errors follow:");
    //                    int errorCount = 0;
    //                    // Log any model errors
    //                    foreach (IError err in model.Errors)
    //                    {
    //                        Console.WriteLine($"  {++errorCount}. Error={err.ErrorText} Object={err.ObjectName} Type={err.ObjectType} Property: Name={err.PropertyName} Value={err.PropertyValue}");
    //                    }
    //                    return null;
    //                }
    //            }
    //            else // model is null
    //            {
    //                explanation = $"Model={modelName} could not be loaded from Project={project.Name}";
    //                return null;
    //            }

    //            return model;
    //        }
    //        catch (Exception ex)
    //        {
    //            explanation = $"Cannot load={modelName} Err={ex.Message}";
    //            return null;
    //        }
    //    }

    //    /// <summary>
    //    /// Load the experiment with name 'experimentName' from the given model.
    //    /// Returns an Experiment object or a null if errors.
    //    /// </summary>
    //    /// <param name="model">The model object; see LoadModel</param>
    //    /// <param name="experimentName">The name of an experiment with model</param>
    //    private static IExperiment LoadExperiment(IModel model, string experimentName, out string explanation)
    //    {
    //        explanation = "";
    //        string marker = "Begin.";

    //        if (model == null)
    //        {
    //            explanation = "Cannot load Experiment: Model is null.";
    //            return null;
    //        }

    //        try
    //        {
    //            marker = $"Loading Experiment={experimentName}";

    //            // Get the experiment from within the Model
    //            var experiment = model.Experiments[experimentName];
    //            if (experiment == null)
    //            {
    //                explanation = $"Experiment={experimentName} Not Found within Model={model.Name}";
    //                return null;
    //            }

    //            return experiment;
    //        }
    //        catch (Exception ex)
    //        {
    //            explanation = $"Cannot load={experimentName} Err={ex.Message}";
    //            return null;
    //        }
    //    }


    //}
    #endregion
}
