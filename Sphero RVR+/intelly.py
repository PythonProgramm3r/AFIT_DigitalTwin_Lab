import sys  # Allows us to use sys.path.append below
import time # Allows us to insert delays in our script

# sys.path tells the interpreter where to look for modules.
# Tell it to look in the SDK directory as well.
sys.path.append('/home/pi/sphero-sdk-raspberrypi-python')

# Now that it can find the SDK, we can import some useful SDK modules
from sphero_sdk import SpheroRvrObserver

# Instantiate (create) a SpheroRvrObserver object
rvr = SpheroRvrObserver()

# This tells the Python interpreter that we are defining 
# a function named main that takes no arguments
def main():

    # Make sure RVR is awake and ready to receive commands
    rvr.wake()
    
    # Wait for RVR to wake up
    time.sleep(2)

    # Resetting our heading makes the current heading 0
    rvr.drive_control.reset_heading()

    #set a speed initially by command line else default to 70
    if sys.argv[1] is None:
        velocity = 70
    else:
        velocity = sys.argv[1]
        
    if sys.argv[2] is None:
        distance = sys.argv[2]
    else:
        distance = 5

    # This helper method drives RVR forward on the specified heading
    # and returns to our main function when the specified time has
    # elapsed.  This means it is a "blocking" function.
    rvr.drive_control.drive_forward_seconds(
        speed=64,       # This is out of 255, where 255 corresponds to 2 m/s
        heading=0,      # Valid heading values are 0-359
        time_to_drive= distance # Driving duration in seconds
    )

    # Now back up.
    rvr.drive_control.drive_backward_seconds(
        speed=64,       # This is out of 255, where 255 corresponds to 2 m/s
        heading=0,      # Valid heading values are 0-359
        time_to_drive= distance # Driving duration in seconds
    )


# This tells the interpreter to run our main() function if it is running this script 
# directly (and not importing it as a module from elsewhere)
if __name__ == '__main__':
    try:
        # Stuff we want to do (in this case, just call our main function)
        main()

    except KeyboardInterrupt:
        # What to do if there's a keyboard interrupt (ctrl+c) exception
        # In this case, we're just going to print a message
        print('\nProgram terminated with keyboard interrupt.')

    finally:
        # What to do before we exit the block
        rvr.close()