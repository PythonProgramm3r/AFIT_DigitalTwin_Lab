import socket, os
def procqit(name):
    try:
        # iterating through each instance of the process
        for line in os.popen("ps ax | grep " + name + " | grep -v grep"):
            fields = line.split()
            # extracting Process ID from the output
            pid = fields[0]
            #print(str(fields))
            # terminating process
            os.kill(int(pid), signal.SIGKILL)
            print("Process Successfully terminated %s"%(fields[0]))
            
    except:
        print("Error Encountered while running script")
        
#initial Robot command
predata = "parked 0"
HOST = "192.168.1.128"  # The server's hostname or IP address
PORT = 5900  # The port used by the server

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
    s.connect((HOST, PORT))
    s.sendall(b"Hello, world")
    while true:
        data = s.recv(1024)
        if(predata != data)
            procqit("drivesim.py")
            robocmd = "sudo python3 drivesim "+ data
            os.system(robocmd)
            predata = data
        print(f"Recieved {data!r}")