'''
    Line Following program for Picar-X:

    Pay attention to modify the reference value of the grayscale module 
    according to the practical usage scenarios.Use the following: 
        px.grayscale.reference = 1400 
    or
        px.set_grayscale_reference(1400)  

'''
from picarx import Picarx
from time import sleep
import sys
px = Picarx()
px = Picarx(grayscale_pins=['A0', 'A1', 'A2']) 
px.set_grayscale_reference(1700)  
px.grayscale.reference = 1700  
last_state = None
current_state = None
px_power = 0
offset = 20
timer = 0
distance = 0
value_list = []
l_sensor = []
m_sensor = []
r_sensor = []
def getCommand():
    name = "not run"
    while name != "run" and name != "stop":
        name = input("please enter command: ")
    return 2

if __name__=='__main__':
    getCommand()
    print(2)



                
