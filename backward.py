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
timer = .002
distance = 0
green = [*range(320,380,1)]
black = [*range(0,94,1)]
white = [*range(220,302,1)]
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
        px.backward(10)
    finally:
        px.stop()


                
