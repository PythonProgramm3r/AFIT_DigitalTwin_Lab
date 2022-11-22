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
        while distance<int(7000):
            gm_val_list = px.get_grayscale_data()
            gm_state = px.get_line_status(gm_val_list)
            #print("gm_val_list: %s, %s"%(gm_val_list, gm_state))
            value_list.append(gm_val_list[0])
            value_list.append(gm_val_list[1])
            value_list.append(gm_val_list[2])
            l_sensor.append(gm_val_list[0])
            m_sensor.append(gm_val_list[1])
            r_sensor.append(gm_val_list[2])
            if(distance%4):
                print("Reading.")
            elif(distance%2):
                print("Reading..")
            else:
                print("Reading...")
            distance += 1
        value_list.sort()
        r_sensor.sort()
        l_sensor.sort()
        m_sensor.sort()
        print("All Sensor Value List : Small : %s Large : %s"%(value_list[0],value_list[-1]))
        print("L Sensor Value List : Small : %s Large : %s"%(l_sensor[0],l_sensor[-1]))
        print("R Sensor Value List : Small : %s Large : %s"%(r_sensor[0],r_sensor[-1]))
        print("M Sensor Value List : Small : %s Large : %s"%(m_sensor[0],m_sensor[-1]))
    finally:
        px.stop()


                
