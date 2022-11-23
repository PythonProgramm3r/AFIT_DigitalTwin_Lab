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
px_power = int(sys.argv[1])
offset = 20
timer = sys.argv[2]
distance = 0
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


if __name__=='__main__':
    try:
        robot_comm = open('/home/pi/picar-x/example/robot_comm.txt','r')
        comm_batch = robot_comm.readlines()
        for instruction in comm_batch:
            instruct = instruction.strip().split()
            print("Instruction: %s"%(instruction))
            px_power = int(instruct[0])
            timer = int(instruct[1])
            print("Power:%s \n Timer:%s"%(px_power,timer))
            while distance<int(instruct[2]):
                px.set_dir_servo_angle(-20)
                #px.forward(px_power) 
                gm_state = 'right'
                #print("gm_val_list: %s, %s"%(gm_val_list, gm_state))
                last_state = 'right'
                if gm_state == 'forward':
                    px.set_dir_servo_angle(0)
                    #px.forward(px_power) 
                elif gm_state == 'right':
                    px.set_dir_servo_angle(-offset)
                    #px.forward(px_power) 
                else:
                    outHandle()
                distance += 1
    finally:
        px.stop()


                
