using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public class GlobalVars : MonoBehaviour
{
    public static int BallsInSystem = 0;
    public static int BallsWaiting = 0;
    public static int BallsProcessed = 0;
    public static int CarsAvailable = 2;
    public static int CarsInUse = 0;
    public static int CarsOnTrack = 0;
    public static int jobsInProcess = 0;
    public static int BallsClaimed = 0;
    public static double CarSpeed = 2.0;
    public static float InterarrivalTime = 1.0f;
    public static double AvgTIS;
    public static double TrueMean = 3;
    public static int TrueMax = 4;
    public static int TrueMin = 0;
    public static double ObservedMean;
    public static double ObservedMax;
    public static double ObservedMin;
    public static double RechargeLevel = 40;
    public static double BPEnd_Forecast;
    public static double BPEnd_Actual;
    public static bool CheckMasterList = false;
    public static bool AllowBattery = true;
    public static bool PrintOutput = false;


    public TextMeshProUGUI AvgTISUIText;
    public TextMeshProUGUI speedUIText;
    public TextMeshProUGUI carsUIText;
    public TextMeshProUGUI rechargeUIText;
    public TextMeshProUGUI BPEndForecastUIText;
    public TextMeshProUGUI BPEndActualUIText;

    public static bool Car2Idle = true;
    public static bool Car3Idle = true;
    public static bool Car4Idle = true;
    public static bool Car5Idle = true;
    public static bool AllowAnchorEdits = false;
    public static bool WPSaved_2 = false;
    public static bool CarChange = false;
    public static bool Running = false;
    public static bool Snap = false;
    public static bool SourceSnapped = false;
    public static bool WP1Snapped = false;
    public static bool WP2Snapped = false;
    public static bool WP3Snapped = false;
    public static bool WP4Snapped = false;
    public static bool WP5Snapped = false;
    public static bool WP6Snapped = false;
    public static bool WP7Snapped = false;

    public static bool E1Snapped = false;
    public static bool E2Snapped = false;
    public static bool E3Snapped = false;
    public static bool E4Snapped = false;
    public static bool E5Snapped = false;

    public static bool H1Snapped = false;
    public static bool H2Snapped = false;
    public static bool H3Snapped = false;
    public static bool H4Snapped = false;
    public static bool H5Snapped = false;

    public static bool X1Snapped = false;
    public static bool X2Snapped = false;
    public static bool X3Snapped = false;
    public static bool X4Snapped = false;
    public static bool X5Snapped = false;

    public static double NextJob;
    public static int NextCar = 0;
    public static int CarChangeBuffer;
    private int lastJobCount = 0;
    private int lastTISCount = 0;
    private double lastCarSpeed = 0;
    private int lastCarsAvailable = 0;
    private double lastRechargeLevel = 0;
    private double lastBPEnd_F = 0;
    private double lastBPEnd_A = 0;
    private double Car1Score;
    private double timeOfSimioRequest;
    private double timeOfSimioResponse;
    private int steadyState_car;
    private double steadyState_speed;
    private double steadyState_recharge;
    private bool SimioRequested = false;
    private bool SimioReceived = false;
    private bool BPComplete = false;
    private bool SteadyStateComplete = false;
    private bool SimioComplete = false;

    private bool Car1LastAssignmentStatus = true;
    private bool Car2LastAssignmentStatus = true;
    private bool Car3LastAssignmentStatus = true;
    private bool Car4LastAssignmentStatus = true;
    private bool Car5LastAssignmentStatus = true;

    //public TextMeshProUGUI BISUIText;

    public static List<GameObject> AllBalls = new List<GameObject>();
    public static SortedDictionary<double, int> AllJobs = new SortedDictionary<double, int>();
    public static SortedDictionary<double, GameObject> AllJobs_time = new SortedDictionary<double, GameObject>();
    public static SortedDictionary<double, int> TrackOrder = new SortedDictionary<double, int>();
    public static Dictionary<GameObject, double> TimeIn = new Dictionary<GameObject, double>();
    public static SortedDictionary<int, double> TimeInSystem = new SortedDictionary<int, double>();
    public static SortedDictionary<double, double> AvgTISCollection = new SortedDictionary<double, double>();
    public static SortedDictionary<double, double> BallsWaitingCollection = new SortedDictionary<double, double>();
    public static List<double> Arrivals = new List<double>();


    private SortedDictionary<double, int> JobChanges = new SortedDictionary<double, int>();
    private Dictionary<int, double> CarScores = new Dictionary<int, double>();

    

    //public static Dictionary<int, List<GameObject>> CarJobs = new Dictionary<int, List<GameObject>>();
    void Start()
    {
        //Print CarsAvailable.
        //Debug.Log("Local Controller: Only " + CarsAvailable + " car(s) available for work.");
        
        //Add all cars to CarScores Dictionary, but we'll only select based on CarsAvailable.
        //CarScores.Add(1, 0);
        //CarScores.Add(2, 0);
        //CarScores.Add(3, 0);
        //CarScores.Add(4, 0);
        //CarScores.Add(5, 0);
    }

    void Update()
    {
        //Update TIS on UI
        if (lastTISCount != TimeInSystem.Count)
        {
            AvgTISUIText.text = AvgTIS.ToString("0.##");
            lastTISCount = TimeInSystem.Count;

            if (AvgTIS != 0)
            {
                AvgTISCollection.Add(Time.timeAsDouble, AvgTIS);
            }
            
            //Check if we are above our threshold
            if (AvgTIS >= 10 && !SimioRequested)
            {
                //Build the message to send to the server.
                string message = ($"{ObservedMin.ToString()}#{ObservedMax.ToString()}#{BallsWaiting.ToString()}");

                //Send current data to Simio
                SimioRequested = true;
                timeOfSimioRequest = Time.timeAsDouble;
                Debug.Log($"{timeOfSimioRequest:f2}: Sending data to Simio...");
                StartClient(message);
                
            }
        }

        if (PrintOutput)
        {
            PrintOutput = false;
            string message = messageBuilder(BallsWaitingCollection);//AvgTISCollection
            StartClient(message);
        }

        if (SimioReceived)
        {
            BPEnd_Forecast += Time.timeAsDouble;
            Debug.Log($"{Time.timeAsDouble:f2}: BusyPeriod decision received.");
            SimioReceived = false;
        }

        if (!BPComplete && SimioRequested && BallsWaiting <= 1)
        {
            BPEnd_Actual = Time.timeAsDouble;
            BPComplete = true;
            BPEndActualUIText.text = BPEnd_Actual.ToString("f2");
            Debug.Log($"{Time.timeAsDouble:f2}: BusyPeriod completed at {BPEnd_Actual}.");
        }

        if (!SimioComplete && BPComplete && SteadyStateComplete)
        {
            CarsAvailable = steadyState_car;
            CarSpeed = steadyState_speed;
            RechargeLevel = steadyState_recharge;
            Debug.Log($"{Time.timeAsDouble:f2}: Implemented steady-state decision. Cars: {CarsAvailable}. Speed: {CarSpeed}. RechargeLevel: {RechargeLevel}");
            BPComplete = false;
            SteadyStateComplete = false;
            SimioComplete = true;
        }

        if (lastCarSpeed != CarSpeed)
        {
            speedUIText.text = CarSpeed.ToString("f1");
            lastCarSpeed = CarSpeed;
        }
        if (lastCarsAvailable != CarsAvailable)
        {
            carsUIText.text = CarsAvailable.ToString();
            lastCarSpeed = CarsAvailable;
        }
        if (lastRechargeLevel != RechargeLevel)
        {
            rechargeUIText.text = RechargeLevel.ToString("f0");
            lastCarSpeed = RechargeLevel;
        }
        if (lastBPEnd_F != BPEnd_Forecast)
        {
            BPEnd_Forecast += timeOfSimioResponse;
            BPEndForecastUIText.text = BPEnd_Forecast.ToString("f2");
            lastBPEnd_F = BPEnd_Forecast;
        }
        //if (lastBPEnd_A != BPEnd_Actual)
        //{
        //    BPEndActualUIText.text = BPEnd_Actual.ToString("f2");
        //    lastBPEnd_A = BPEnd_Actual;
        //}

        //Detect AllowAssignment change.
        if (Car1LastAssignmentStatus != Car1Behavior.AllowAssignment)
        {
            //Reassign Car1 workload to other available cars.
            //Debug.Log("Local controller: Car1 status change detected. Reassigning workload...");
            //Clear all pending work.
            ClearPending();

            //Redistribute workload.
            RedistributeWorkload(AllJobs);

            Car1LastAssignmentStatus = Car1Behavior.AllowAssignment;
        }

        if (Car2LastAssignmentStatus != Car2Behavior.AllowAssignment)
        {
            //Reassign Car2 workload to other available cars.
            //Debug.Log("Local controller: Car2 status change detected. Reassigning workload...");
            //Clear all pending work.
            ClearPending();

            //Redistribute workload.
            RedistributeWorkload(AllJobs);

            Car2LastAssignmentStatus = Car2Behavior.AllowAssignment;
        }

        if (Car3LastAssignmentStatus != Car3Behavior.AllowAssignment)
        {
            //Reassign Car3 workload to other available cars.
            //Debug.Log("Local controller: Car3 status change detected. Reassigning workload...");
            //Clear all pending work.
            ClearPending();

            //Redistribute workload.
            RedistributeWorkload(AllJobs);

            Car3LastAssignmentStatus = Car3Behavior.AllowAssignment;
        }

        if (Car4LastAssignmentStatus != Car4Behavior.AllowAssignment)
        {
            //Reassign Ca4 workload to other available cars.
            //Debug.Log("Local controller: Car4 status change detected. Reassigning workload...");
            //Clear all pending work.
            ClearPending();

            //Redistribute workload.
            RedistributeWorkload(AllJobs);

            Car4LastAssignmentStatus = Car4Behavior.AllowAssignment;
        }

        if (Car5LastAssignmentStatus != Car5Behavior.AllowAssignment)
        {
            //Reassign Car5 workload to other available cars.
            //Debug.Log("Local controller: Car5 status change detected. Reassigning workload...");
            //Clear all pending work.
            ClearPending();

            //Redistribute workload.
            RedistributeWorkload(AllJobs);

            Car5LastAssignmentStatus = Car5Behavior.AllowAssignment;
        }

        //Detect a job arrival or completion.
        if (lastJobCount != AllJobs.Count)
        {
            //Debug.Log("AllJobs change detected. Current count: " + AllJobs.Count);
            //Job change detected.
            if (AllJobs.Count > 0)
            {
                foreach (KeyValuePair<double, int> item in AllJobs)
                {
                    NextJob = item.Key;

                    if (item.Value == 0)
                    {
                        //This is an arrival (car has not been assigned).
                        NextCar = 0;
                        //Debug.Log("Local Controller: Job-arrival detected. NextJob: " + NextJob + ". Assigning car to job...");
                    }
                    else
                    {
                        //This is a job completion (car already assigned)
                        NextCar = item.Value;
                        //Debug.Log("Local Controller: Job-completion detected. NextJob: " + NextJob + ". NextCar: " + NextCar);
                    }
                    
                    //Break here, so we only get the first item.
                    break;
                }
            }
            else
            {
                //Debug.Log("Local Controller: No pending jobs.");
            }

            lastJobCount = AllJobs.Count;

        }

        if (AllJobs.ContainsValue(0))
        {
            //Debug.Log("Local Controller: New arrival. Selecting car with least laps (pending and completed).");

            //Copy AllJobs dictionary to modify in foreach
            JobChanges = new SortedDictionary<double, int>(AllJobs);
            
            //Assign ball to car logic
            foreach (KeyValuePair<double, int> entry in JobChanges)
            {
                if (entry.Value == 0)
                {
                    AllJobs[entry.Key] = SelectCar(entry.Key, AllJobs_time[entry.Key], CarScores);

                    //if (AllJobs[entry.Key] == 1)
                    //{
                    //    Car1Behavior.Jobs.Add(entry.Key, AllJobs_time[entry.Key]);
                    //    Car1Behavior.LapsPending++;
                    //}
                    //else if (AllJobs[entry.Key] == 2)
                    //{
                    //    Car2Behavior.Jobs.Add(entry.Key, AllJobs_time[entry.Key]);
                    //    Car2Behavior.LapsPending++;
                    //}
                    //else if (AllJobs[entry.Key] == 3)
                    //{
                    //    Car3Behavior.Jobs.Add(entry.Key, AllJobs_time[entry.Key]);
                    //    Car3Behavior.LapsPending++;
                    //}
                    //else if (AllJobs[entry.Key] == 4)
                    //{
                    //    Car4Behavior.Jobs.Add(entry.Key, AllJobs_time[entry.Key]);
                    //    Car4Behavior.LapsPending++;
                    //}
                    //else if (AllJobs[entry.Key] == 5)
                    //{
                    //    Car5Behavior.Jobs.Add(entry.Key, AllJobs_time[entry.Key]);
                    //    Car5Behavior.LapsPending++;
                    //}



                    //CarsInUse++;
                    //Debug.Log("Local Controller: Job assigned to Car" + AllJobs[entry.Key] + ". CarsInUse: " + CarsInUse + ". CarsAvailable: " + CarsAvailable);

                    //Detect if this was the first assignment. If so, assign the car.
                    //if (NextCar == 0)
                    //{
                    //    NextCar = AllJobs[entry.Key];
                    //    //Debug.Log("Local Controller: NextCar was not assigned. NextCar: " + NextCar);
                    //}
                }
                else
                {
                    //Do nothing (ball already assigned to car)
                }

                
            }
            //int i = 1;
            //foreach (KeyValuePair<double, GameObject> entryTime in AllJobs_time)
            //{
            //    Debug.Log("Local Controller: Current jobs listed by entry time. Job#: " + i.ToString() + " Time entered: " + entryTime.Key);
            //    i++;
            //}
        }
        
        
    }

    private void ClearPending()
    {
        //Clear pending so we can redistribute workload.

        //Clear any pending jobs from the car's local list
        Car1Behavior.Jobs.Clear();

        //If the car is currently processing a ball, reset LapsPending to 1 (to avoid negative numbers from the completion decrement)
        if (Car1Behavior.ProcessingBall)
            Car1Behavior.LapsPending = 1;
        else
            Car1Behavior.LapsPending = 0;

        Car2Behavior.Jobs.Clear();
        if (Car2Behavior.ProcessingBall)
            Car2Behavior.LapsPending = 1;
        else
            Car2Behavior.LapsPending = 0;

        Car3Behavior.Jobs.Clear();
        if (Car3Behavior.ProcessingBall)
            Car3Behavior.LapsPending = 1;
        else
            Car3Behavior.LapsPending = 0;

        Car4Behavior.Jobs.Clear();
        if (Car4Behavior.ProcessingBall)
            Car4Behavior.LapsPending = 1;
        else
            Car4Behavior.LapsPending = 0;

        Car5Behavior.Jobs.Clear();
        if (Car5Behavior.ProcessingBall)
            Car5Behavior.LapsPending = 1;
        else
            Car5Behavior.LapsPending = 0;
    }

    private void RedistributeWorkload(SortedDictionary<double, int> AllJobsDict)
    {
        //Copy AllJobs dictionary to modify in foreach
        JobChanges = new SortedDictionary<double, int>(AllJobsDict);

        foreach (KeyValuePair<double, int> item in JobChanges)
        {
            AllJobsDict[item.Key] = SelectCar(item.Key, AllJobs_time[item.Key], CarScores);
        }

        //Debug.Log("Local controller: Redistribution complete.");
    }
   

    private int SelectCar(double entryTime, GameObject ball, Dictionary<int, double> CarScoresDict)
    {
        int minKey = 0;
        double carScore = 0;
        int totalLaps = 0;
        int i = 1;
        //Calculate scores based on number of jobs pending in each car's job list.
        if (Car1Behavior.AllowAssignment)
        {
            if (Car1Behavior.LapsPending == 0)
            {
                //Car needs job. Save laps completed. Ignore if LapsPending is > 1 (car is processing).
                CarScoresDict.Add(1, Car1Behavior.LapsCompleted);
                //Debug.Log("Car 1 not processing. LapsCompleted: " + Car1Behavior.LapsCompleted);
            }
        }
        if (Car2Behavior.AllowAssignment)
        {
            if (Car2Behavior.LapsPending == 0)
            {
                //Car needs job. Save laps completed. Ignore if LapsPending is > 1 (car is processing).
                CarScoresDict.Add(2, Car2Behavior.LapsCompleted);
                //Debug.Log("Car 2 not processing. LapsCompleted: " + Car2Behavior.LapsCompleted);
            }
        }
        if (Car3Behavior.AllowAssignment)
        {
            if (Car3Behavior.LapsPending == 0)
            {
                //Car needs job. Save laps completed. Ignore if LapsPending is > 1 (car is processing).
                CarScoresDict.Add(3, Car3Behavior.LapsCompleted);
            }
        }
        if (Car4Behavior.AllowAssignment)
        {
            if (Car4Behavior.LapsPending == 0)
            {
                //Car needs job. Save laps completed. Ignore if LapsPending is > 1 (car is processing).
                CarScoresDict.Add(4, Car4Behavior.LapsCompleted);
            }
        }
        if (Car5Behavior.AllowAssignment)
        {
            if (Car5Behavior.LapsPending == 0)
            {
                //Car needs job. Save laps completed. Ignore if LapsPending is > 1 (car is processing).
                CarScoresDict.Add(5, Car5Behavior.LapsCompleted);
            }
        }

        if (CarScoresDict.Count == 0)
        {
            //All cars are either busy or offline. Check for all busy first.

            if (Car1Behavior.AllowAssignment)
            {
                carScore = Car1Behavior.LapsPending;
                CarScoresDict.Add(1, carScore);
                //Debug.Log("Car 1 Busy. LapsPending: " + Car1Behavior.LapsPending + " LapsCompleted: " + Car1Behavior.LapsCompleted);
            }
            if (Car2Behavior.AllowAssignment)
            {
                carScore = Car2Behavior.LapsPending;
                CarScoresDict.Add(2, carScore);
                //Debug.Log("Car 2 Busy. LapsPending: " + Car2Behavior.LapsPending + " LapsCompleted: " + Car2Behavior.LapsCompleted);
            }
            if (Car3Behavior.AllowAssignment)
            {
                carScore = Car3Behavior.LapsPending;
                CarScoresDict.Add(3, carScore);
            }
            if (Car4Behavior.AllowAssignment)
            {
                carScore = Car4Behavior.LapsPending;
                CarScoresDict.Add(4, carScore);
            }
            if (Car5Behavior.AllowAssignment)
            {
                carScore = Car5Behavior.LapsPending;
                CarScoresDict.Add(5, carScore);
            }
        }

        if (CarScoresDict.Count == 0)
        {
            //All cars are offline for something. Wait to reassign.
            return 0;
        }
        
        foreach (KeyValuePair<int, double> score in CarScoresDict)
        {
            if (i == 1)
            {
                carScore = score.Value;
                minKey = score.Key;
            }
            else if (score.Value < carScore)
            {
                carScore = score.Value;
                minKey = score.Key;
            }
            i++;

        }
        //Debug.Log(Time.timeAsDouble.ToString("f2") + " Local Controller: Selected Car" + minKey + " for work. Value: " + carScore);

        //Assign specific ball to selected car
        if (minKey == 1)
        {
            Car1Behavior.Jobs.Add(entryTime, ball);
            Car1Behavior.LapsPending++;
        }
        else if (minKey == 2)
        {
            Car2Behavior.Jobs.Add(entryTime, ball);
            Car2Behavior.LapsPending++;
        }
        else if (minKey == 3)
        {
            Car3Behavior.Jobs.Add(entryTime, ball);
            Car3Behavior.LapsPending++;
        }
        else if (minKey == 4)
        {
            Car4Behavior.Jobs.Add(entryTime, ball);
            Car4Behavior.LapsPending++;
        }
        else if (minKey == 5)
        {
            Car5Behavior.Jobs.Add(entryTime, ball);
            Car5Behavior.LapsPending++;
        }
        else
        {
            Debug.Log("Local Controller: ERROR: No car selected.");
        }
        CarScoresDict.Clear();
        return minKey;

    }

    private string messageBuilder(SortedDictionary<double, double> collection)
    {
        int i = 1;
        StringBuilder output = new System.Text.StringBuilder();
        foreach (KeyValuePair<double,double> item in collection)
        {
            if (i == 1)
            {
                output.Append($"{item.Key:f2}#{item.Value:f2}");
            }
            else
            {
                output.Append($"@{item.Key:f2}#{item.Value:f2}");
            }
            i++;
        }
        return output.ToString();
    }

    //// The port number for the remote device.  
    //private const int port = 11000;

    //// ManualResetEvent instances signal completion.  
    //private static ManualResetEvent connectDone =
    //    new ManualResetEvent(false);
    //private static ManualResetEvent sendDone =
    //    new ManualResetEvent(false);
    //private static ManualResetEvent receiveDone =
    //    new ManualResetEvent(false);

    //// The response from the remote device.  
    //private static String response = String.Empty;

    ////// Client socket.  
    ////public Socket workSocket = null;
    ////// Size of receive buffer.  
    ////public const int BufferSize = 256;
    ////// Receive buffer.  
    ////public byte[] buffer = new byte[BufferSize];
    ////// Received data string.  
    ////public StringBuilder sb = new StringBuilder();

    ////private IEnumerator coroutine;

    //public void StartMe()
    //{
    //    //Thread t1 = new Thread(StartClient());
    //    //coroutine = StartClient();
    //    StartCoroutine(StartClient());
    //    Debug.Log("Client started...");
    //    // t1.Start();
    //}

    //private IEnumerator StartClient()
    //{
    //    Debug.Log("Coroutine started.");
    //    // Connect to a remote device.  
    //    try
    //    {


    //        // Establish the remote endpoint for the socket.  
    //        // The name of the
    //        // remote device is "host.contoso.com".  
    //        //IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
    //        //IPAddress ipAddress = ipHostInfo.AddressList[0];
    //        IPAddress ipAddress = IPAddress.Parse("172.24.1.218");
    //        Debug.Log("ipAddress: " + ipAddress);
    //        //return;
    //        IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

    //        // Create a TCP/IP socket.  
    //        Socket client = new Socket(ipAddress.AddressFamily,
    //            SocketType.Stream, ProtocolType.Tcp);

    //        // Connect to the remote endpoint.  
    //        client.BeginConnect(remoteEP,
    //            new AsyncCallback(ConnectCallback), client);
    //        connectDone.WaitOne();

    //        // Send test data to the remote device.  
    //        Send(client, "Run Simio<EOF>");
    //        sendDone.WaitOne();

    //        // Receive the response from the remote device.  
    //        Receive(client);
    //        receiveDone.WaitOne();

    //        // Write the response to the console.  
    //        Debug.Log("Response received : " + response);

    //        // Release the socket.  
    //        client.Shutdown(SocketShutdown.Both);
    //        client.Close();

    //    }
    //    catch (Exception e)
    //    {
    //        Debug.Log(e.ToString());
    //    }
    //    yield return null;
    //}

    //private static void ConnectCallback(IAsyncResult ar)
    //{
    //    try
    //    {
    //        // Retrieve the socket from the state object.  
    //        Socket client = (Socket)ar.AsyncState;

    //        // Complete the connection.  
    //        client.EndConnect(ar);

    //        Debug.Log("Socket connected to " + client.RemoteEndPoint.ToString());

    //        // Signal that the connection has been made.  
    //        connectDone.Set();
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.Log(e.ToString());
    //    }
    //}

    //private static void Receive(Socket client)
    //{
    //    try
    //    {
    //        // Create the state object.  
    //        StateObject state = new StateObject();
    //        state.workSocket = client;

    //        // Begin receiving the data from the remote device.  
    //        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
    //            new AsyncCallback(ReceiveCallback), state);
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.Log(e.ToString());
    //    }
    //}

    //private static void ReceiveCallback(IAsyncResult ar)
    //{
    //    try
    //    {
    //        // Retrieve the state object and the client socket
    //        // from the asynchronous state object.  
    //        StateObject state = (StateObject)ar.AsyncState;
    //        Socket client = state.workSocket;

    //        // Read data from the remote device.  
    //        int bytesRead = client.EndReceive(ar);

    //        if (bytesRead > 0)
    //        {
    //            // There might be more data, so store the data received so far.  
    //            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

    //            // Get the rest of the data.  
    //            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
    //                new AsyncCallback(ReceiveCallback), state);
    //        }
    //        else
    //        {
    //            // All the data has arrived; put it in response.  
    //            if (state.sb.Length > 1)
    //            {
    //                response = state.sb.ToString();
    //            }
    //            // Signal that all bytes have been received.  
    //            receiveDone.Set();
    //        }
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.Log(e.ToString());
    //    }
    //}

    //private static void Send(Socket client, String data)
    //{
    //    // Convert the string data to byte data using ASCII encoding.  
    //    byte[] byteData = Encoding.ASCII.GetBytes(data);

    //    // Begin sending the data to the remote device.  
    //    client.BeginSend(byteData, 0, byteData.Length, 0,
    //        new AsyncCallback(SendCallback), client);
    //}

    //private static void SendCallback(IAsyncResult ar)
    //{
    //    try
    //    {
    //        // Retrieve the socket from the state object.  
    //        Socket client = (Socket)ar.AsyncState;

    //        // Complete sending the data to the remote device.  
    //        int bytesSent = client.EndSend(ar);
    //        Debug.Log("Sent " + bytesSent + " bytes to server.");

    //        // Signal that all bytes have been sent.  
    //        sendDone.Set();
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.Log(e.ToString());
    //    }
    //}

    public void StartClient(string message)
    {
        new Thread(() => {
            Thread.CurrentThread.IsBackground = true;
            ConnectClient("172.24.1.218", 11000, message); //"172.24.1.218"
        }).Start();
    }

    private void ConnectClient(string server, int port, string message)
    {
        try
        {
            TcpClient client = new TcpClient(server, port);
            NetworkStream stream = client.GetStream();
            int count = 0;

            while (count++ < 1)
            {
                byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                stream.Write(data, 0, data.Length);
                Debug.Log($"Sent: {message}");

                if (message.Contains("@"))
                {
                    //This is an end-of-run data download. Disconnect client.
                    stream.Close();
                    client.Close();
                    Debug.Log("Disconnected client at end-of-run.");
                }

                data = new byte[1024];
                bool scan = true;
                int bytes;
                string response;
                string[] responseValues;

                while (scan)
                {
                    bytes = stream.Read(data, 0, data.Length);
                    Debug.Log("Message received. Parsing...");
                    response = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                    responseValues = response.Split('#');

                    if (responseValues[0] == "Stage1")
                    {
                        SimioReceived = true;
                        BPEnd_Forecast = Convert.ToDouble(responseValues[1]);
                        CarsAvailable = Int16.Parse(responseValues[3]);
                        CarSpeed = Convert.ToDouble(responseValues[4]);
                        RechargeLevel = Int16.Parse(responseValues[5]);
                        responseValues = null;
                        Debug.Log($"Received BP decision. CarsToUse: {CarsAvailable}. CarSpeed: {CarSpeed}. RechargeLevel: {RechargeLevel}.");
                    }
                    else if (responseValues[0] == "Stage2")
                    {
                        steadyState_car = Int16.Parse(responseValues[3]);
                        steadyState_speed = Convert.ToDouble(responseValues[4]);
                        steadyState_recharge = Int16.Parse(responseValues[5]);
                        SteadyStateComplete = true;
                        Debug.Log($"Received steady-state decision. CarsToUse: {CarsAvailable}. CarSpeed: {CarSpeed}. RechargeLevel: {RechargeLevel}");
                        scan = false;
                    }
                }

                Debug.Log("All messages received. Disconnecting...");

                //int bytes = stream.Read(data, 0, data.Length);
                //Debug.Log("Pos2");
                //string response = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //Debug.Log("Pos3");
                //string[] responseValues = response.Split('#');
                //This response is structured with stage number at first index.
                //if (responseValues[0] == "Stage1")
                //{
                //    CarSpeed = Convert.ToDouble(responseValues[3]);
                //    CarsAvailable = Int16.Parse(responseValues[4]);
                //    RechargeLevel = Int16.Parse(responseValues[5]);
                //    Debug.Log($"Received BP decision. CarsToUse: {CarsAvailable}. CarSpeed: {CarSpeed}. RechargeLevel: {RechargeLevel}");
                //}
                
               // Debug.Log($"Received: {response}");

                //Thread.Sleep(2000);
            }

            stream.Close();
            client.Close();
            Debug.Log("Disconnected client.");
        }
        catch (Exception e)
        {
            Debug.Log($"Exception : {e}");
        }

        //Console.Read();
    }


}

// State object for receiving data from remote device.  
public class StateObject
{
    // Client socket.  
    public Socket workSocket = null;
    // Size of receive buffer.  
    public const int BufferSize = 256;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.  
    public StringBuilder sb = new StringBuilder();
}


