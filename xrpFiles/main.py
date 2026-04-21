import os
import sys
import time

FILE_PATH = '/lib/ble/isrunning'

# Always reset the flag so our script runs
try:
    with open(FILE_PATH, 'r+b') as file:
        file.seek(0)
        file.write(b'\x00')
except:
    pass

doNothing = False
x = os.dupterm(None, 0)
if x == None:
    import ble.blerepl
else:
    os.dupterm(x, 0)

try:
    with open(FILE_PATH, 'r+b') as file:
        byte = file.read(1)
        if byte == b'\x01':
            file.seek(0)
            file.write(b'\x00')
            doNothing = True

    if not doNothing:
        time.sleep(5)
        with open('xrpgodot.py', mode='r') as exfile:
            code = exfile.read()
        execCode = compile(code, 'xrpgodot.py', 'exec')
        exec(execCode)

except Exception as e:
    sys.print_exception(e)
    try:
        with open('boot_error.log', 'w') as f:
            sys.print_exception(e, f)
    except:
        pass
finally:
    import gc
    gc.collect()
    if 'XRPLib.resetbot' in sys.modules:
        del sys.modules['XRPLib.resetbot']
    import XRPLib.resetbot
