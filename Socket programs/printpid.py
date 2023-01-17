import os , signal , time
os.system('sudo python3 portopn.py')
os.system('echo $!')
printpid = os.getpid()
time.sleep(.07)
os.kill(portpid, signal.SIGSTOP)
print("printpid =",printpid)