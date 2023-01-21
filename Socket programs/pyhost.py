import socket
import sys
# Set up a TCP/IP server
tcp_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
# Bind the socket to server address and port 81
server_address = ("192.168.1.128", 5900)
tcp_socket.bind(server_address)
# Listen on port 81
tcp_socket.listen()
while True:
    print("Waiting for connection")
    connection, client = tcp_socket.accept()
    try:
        print("Connected to client IP: {}".format(client))
        # Receive and print data 32 bytes at a time, as long as the client is sending something
        mssages = ["drive 50", "park 0", "drive 20"]
        i = 0
        while True:
            if(i>2):
                i=0
            msg = mssages[i].encode('utf-8')
            #msg = bytes(mssages[i], 'utf-8')
            connection.sendall(msg)
            data = connection.recv(32)
            decoded = data.decode('utf-8')
            print("Received data: {}".format(decoded))
            i+=1
            
            if not data:
                break
    finally:
            connection.close()
