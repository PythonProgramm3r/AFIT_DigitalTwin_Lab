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
# px = Picarx(grayscale_pins=['A0', 'A1', 'A2']) 
px.set_grayscale_reference(1400)  
px.grayscale.reference = 1400  
last_state = None
current_state = None
px_power = int(sys.argv[1])
offset = 20
timer = sys.argv[2]
distance = 0
green = [*range(270,320,1)]
black = [*range(0,70,1)]
track = [*range(208,242,1)]
print(green)
print(black)
print(track)
def outHandle():
    global last_state, current_state
    if last_state == 'left':
        px.set_dir_servo_angle(-20)
        px.backward(10)
        print("outHandle gm_val_list: %s, %s"%(gm_val_list, gm_state))
    elif last_state == 'right':
        px.set_dir_servo_angle(20)
        px.backward(10)
        print("outHandle gm_val_list: %s, %s"%(gm_val_list, gm_state))
    while True:
        gm_val_list = px.get_grayscale_data()
        gm_state = px.get_line_status(gm_val_list)
        currentSta = gm_state
        if currentSta != last_state:
            break
    sleep(timer)

if __name__=='__main__':
    try:
        while distance < int(sys.argv[3]):
            gm_val_list = px.get_grayscale_data()
            gm_state = px.get_line_status(gm_val_list)
            #print(distance)
            #print("gm_val_list: %s, %s"%(gm_val_list, gm_state))
            if distance > 200 and gm_val_list[1] in green:
                while gm_val_list[1] in green and distance < 2000:
                    gm_val_list = px.get_grayscale_data()
                    gm_state = px.get_line_status(gm_val_list)
                    if gm_state != "stop":
                        last_state = gm_state
                    if gm_val_list[0] in green:
                        print("sl green")
                        #px.set_dir_servo_angle(20)
                        #px.forward(px_power) 
                        gm_state = 'left'
                        last_state = 'left'
                    elif gm_val_list[2] in green:
                        print("sr green")
                        #px.set_dir_servo_angle(-20)
                        #px.forward(px_power) 
                        gm_state = 'right'
                        last_state = 'right'
                    if gm_state == 'forward':
                        #px.set_dir_servo_angle(0)
                        #px.forward(px_power) 
                    elif gm_state == 'left':
                        #px.set_dir_servo_angle(offset)
                        #px.forward(px_power) 
                    elif gm_state == 'right':
                        #px.set_dir_servo_angle(-offset)
                        #px.forward(px_power) 
                    else:
                        outHandle()
                distance += 1
                print("Green Tape")
                print("gm_val_list[1] in green")
                print("Working")
            if distance > 2000:
                distance = 0
            if gm_state != "stop":
                last_state = gm_state
            if (gm_val_list[0] in track) and (gm_val_list[1] in black): #left
                print("gm_val_list[0] in track")
                #px.set_dir_servo_angle(20)
                #px.forward(px_power) 
                gm_state = 'left'
                print("gm_val_list: %s, %s"%(gm_val_list, gm_state))
                last_state = 'left'
            elif (gm_val_list[2] in track) and (gm_val_list[1] in black): #right
                print("gm_val_list[2] in track")
                #px.set_dir_servo_angle(-20)
                #px.forward(px_power) 
                gm_state = 'right'
                #print("gm_val_list: %s, %s"%(gm_val_list, gm_state))
                last_state = 'right'
            if gm_state == 'forward':
                #px.set_dir_servo_angle(0)
                #px.forward(px_power) 
            elif gm_state == 'left':
                #px.set_dir_servo_angle(offset)
                #px.forward(px_power) 
            elif gm_state == 'right':
                #px.set_dir_servo_angle(-offset)
                #px.forward(px_power) 
            else:
                outHandle()
            distance += 1
    finally:
        px.stop()


                
