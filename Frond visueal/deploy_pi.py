#!/usr/bin/env python3
import paramiko, io, time, sys

HOST, USER, PASS = '10.10.20.97', 'zealand', 'dims2025!'

SENSOR_API = open('sensor_api.py').read()

SERVICE = """\
[Unit]
Description=Klima Kontrolloeren Sensor API
After=network.target

[Service]
User=zealand
ExecStart=/usr/bin/python3 /home/zealand/sensor_api.py
Restart=always
RestartSec=5
Environment=PYTHONUNBUFFERED=1

[Install]
WantedBy=multi-user.target
"""

SETUP_SH = f"""\
#!/bin/bash
set -e
echo "--- Enabling I2C ---"
raspi-config nonint do_i2c 0
modprobe i2c-dev 2>/dev/null || true
usermod -a -G i2c zealand 2>/dev/null || true

echo "--- Installing Python packages ---"
pip3 install flask flask-cors scd30-i2c --break-system-packages -q

echo "--- Installing systemd service ---"
cp /tmp/klima-sensor.service /etc/systemd/system/klima-sensor.service
systemctl daemon-reload
systemctl enable klima-sensor
systemctl restart klima-sensor

echo "--- Done ---"
"""

def run(client, cmd, password=None):
    print(f"  $ {cmd}")
    stdin, stdout, stderr = client.exec_command(cmd, get_pty=bool(password))
    if password:
        time.sleep(0.3)
        stdin.write(password + '\n')
        stdin.flush()
    exit_code = stdout.channel.recv_exit_status()
    out = stdout.read().decode().strip()
    err = stderr.read().decode().strip()
    if out:
        for line in out.splitlines():
            print(f"    {line}")
    if err:
        for line in err.splitlines():
            if not any(x in line.lower() for x in ('warning', 'notice', '[sudo]')):
                print(f"    ERR: {line}")
    return exit_code

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
print(f"Connecting to {HOST} ...")
client.connect(HOST, username=USER, password=PASS, timeout=10)
print("Connected.\n")

sftp = client.open_sftp()
sftp.putfo(io.BytesIO(SENSOR_API.encode()), '/home/zealand/sensor_api.py')
sftp.putfo(io.BytesIO(SERVICE.encode()), '/tmp/klima-sensor.service')
sftp.putfo(io.BytesIO(SETUP_SH.encode()), '/tmp/setup.sh')
sftp.close()
print("Files uploaded.\n")

print("[Running setup as sudo]")
code = run(client, f'echo {PASS} | sudo -S bash /tmp/setup.sh')
if code != 0:
    print(f"Setup exited with code {code} — check errors above.")
    sys.exit(1)

print("\n[Service status]")
run(client, 'systemctl is-active klima-sensor')
run(client, 'journalctl -u klima-sensor -n 20 --no-pager')

client.close()
print(f"\nDone! API available at http://{HOST}:5000/klima-data")
