import os, signal
def check_process(name):
    output = []
    cmd = "ps -aef | grep -i '%s' | grep -v 'grep' | awk '{ print $2 }' > /tmp/out"
    os.system(cmd % name)
    with open('/tmp/out', 'r') as f:
        line = f.readline()
        while line:
            output.append(line.strip())
            line = f.readline()
            if line.strip():
                output.append(line.strip())

    return output

run_proc_list = check_process("drivesim.py")
#print(str(run_proc_list))
def process(name):
    # Ask user for the name of process
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
        
process("drivesim.py")