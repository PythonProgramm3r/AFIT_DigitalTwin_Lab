import socket

HOST = "192.168.1.128"  # The server's hostname or IP address
PORT = 5900  # The port used by the server

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
    s.connect((HOST, PORT))
    s.sendall(b"Hello, world")
    while true:
        data = s.recv(1024)
        print(f"Recieved {data!r}")