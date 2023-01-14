import os

print("TCP ports: \n")
os.system('sudo netstat -lnt | grep LISTEN | awk \'{ print ( $4 ) }\' | awk \'BEGIN{FS=":"} { print $(NF) }\' | sort -n | uniq')
print("UDP ports: \n")
os.system("sudo netstat -lnu | grep udp | awk '{ print ( $4 ) }' | awk 'BEGIN{FS=\":\"} { print $(NF) }' | sort -n | uniq")
print("portopn pid = ",str(os.getpid()))