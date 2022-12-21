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
import subprocess
import park
import sys
import os
import time
px = Picarx()
px = Picarx(grayscale_pins=['A0', 'A1', 'A2']) 
px.set_grayscale_reference(1700)  
px.grayscale.reference = 1700  
last_state = None
current_state = None
px_power = int(sys.argv[1])
offset = 20
timer = .002
distance = 0
green = [*range(320,380,1)]
black = [*range(0,94,1)]
white = [*range(220,302,1)]
rb_timer = os.path.getmtime('/home/pi/picar-x/example/robot_comm.txt')
print("Original robot_comm mod timestamp: %s"%(rb_timer))
duration = 120000000

def outHandle():
    global last_state, current_state
    if last_state == 'left':
        px.set_dir_servo_angle(-20)
        px.backward(10)
        #print("outHandle gm_val_list: %s, %s"%(gm_val_list, gm_state))
    elif last_state == 'right':
        px.set_dir_servo_angle(20)
        px.backward(10)
        #print("outHandle gm_val_list: %s, %s"%(gm_val_list, gm_state))
    while True:
        gm_val_list = px.get_grayscale_data()
        gm_state = px.get_line_status(gm_val_list)
        currentSta = gm_state
        if currentSta != last_state:
            break
    sleep(timer)

def newInstruction():
    if rb_timer != os.path.getmtime('/home/pi/picar-x/example/robot_comm.txt'):
        robot_comm = open('/home/pi/picar-x/example/robot_comm.txt','r')
        comm_batch = robot_comm.readlines()
        for instruction in comm_batch:
            instruct = instruction.strip().split()
            print("Instruction: %s"%(instruction))
            px_power = int(instruct[0])
            duration = int(instruct[1])
        rb_timer = os.path.getmtime('/home/pi/picar-x/example/robot_comm.txt')
        robot_comm.close()

if __name__=='__main__':
    try:
        transition = 0
        print(transition)
        onGreen = 1
        while distance<duration:
            gm_val_list = px.get_grayscale_data()
            gm_state = px.get_line_status(gm_val_list)
            #print("gm_val_list: %s, %s"%(gm_val_list, gm_state))
            if gm_val_list[0] in green and gm_val_list[1] in green and gm_val_list[2] in green and onGreen == 1:
                #print("green")
                transition += 1
                #px.set_dir_servo_angle(20)
                #px.forward(-20) 
                #gm_state = 'stop'
                print(transition)
                onGreen = 2
            if gm_state != "stop":
                last_state = gm_state
            if gm_val_list[0] in black : #left
                px.set_dir_servo_angle(20)
                px.forward(px_power) 
                gm_state = 'left'
                #print("gm_val_list[1] in black and transition == 1")
                last_state = 'left'
            elif gm_val_list[2] in black : #right
                px.set_dir_servo_angle(-20)
                px.forward(px_power) 
                gm_state = 'right'
                #print("gm_val_list[2] in black and transition == 1")
                last_state = 'right'
            if (gm_val_list[0] in black and gm_val_list[1] in black and gm_val_list[2] in black):
                px.set_dir_servo_angle(0)
                px.backward(2)
                sleep(.05)
                #while parking == 0:
                #    parking = subprocess.check_output([sys.executable, "park.py", "34"])     
                #    print(parking)
                #new park script
                #park = 0
                #park = os.system("sudo python3 park.py")
                #print("gm_val_list[0] not in black and gm_val_list[1] not in black and gm_val_list[2] not in black %s"%transition)
            if gm_state == 'forward':
                px.set_dir_servo_angle(0)
                px.forward(px_power) 
            elif gm_state == 'left':
                px.set_dir_servo_angle(offset)
                px.forward(px_power) 
            elif gm_state == 'right':
                px.set_dir_servo_angle(-offset)
                px.forward(px_power) 
            else:
                outHandle()
            distance += 1
            newInstruction()
    finally:
        px.stop()


                
