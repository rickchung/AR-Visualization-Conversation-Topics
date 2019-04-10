import socketserver

class MyUDPHandler(socketserver.BaseRequestHandler):
    client_list = {}

    def handle(self):
        data = self.request[0]
        socket = self.request[1]
        address = self.client_address

        # If it is the first time seeing this connection, save it and
        # do nothing.
        if address not in this.client_list:
            print("New connection: " + str(address))
            this.client_list[address] = socket        

        # Otherwise, broadcast the data too all the saved connections 
        else:
            for c_addr, c_socket in this.client_list.items():
                # if c_addr != address
                #     c_socket.sendto(data, c_addr)
                c_socket.sendto(data, c_addr)

if __name__ == "__main__":
    HOST, PORT = "0.0.0.0", 7777
    with socketserver.UDPServer((HOST, PORT), MyUDPHandler) as server:
        server.serve_forever()