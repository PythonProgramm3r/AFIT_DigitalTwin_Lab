from socket import *

host = "192.168.1.128"

print(host)

port=5900

s=socket(AF_INET, SOCK_STREAM)

print("socket made")

s.connect((host,port))

print("socket connected!!!")

msg=s.recv(1024)

print("Message from server : " + msg)