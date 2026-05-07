import scd30_i2c
import time
import threading
import requests
import json
import traceback
import sys
try:

    # URL of the server to send data to
    SERVER_URL = "https://klimakontrolloeren-backend-b8h5g9azhqdjf3gm.norwayeast-01.azurewebsites.net/api/sensor"
    #Setup the sensor
    sensor = scd30_i2c.SCD30()
    sensor.set_measurement_interval(2)
    sensor.start_periodic_measurement()

    _latest = {'co2': None, 'temperature': None, 'humidity': None}
    _lock = threading.Lock()
    _readings = []

    def _poll():
        while True:
            print("Polling sensor for new data...", flush=True)
            try:
                if sensor.get_data_ready():
                    m = sensor.read_measurement()
                    if m is not None:
                        co2, temp, hum = m
                        with _lock:
                            _latest['co2'] = round(float(co2), 1)
                            _latest['temperature'] = round(float(temp), 1)
                            _latest['humidity'] = round(float(hum), 1)
                            _readings.append({
                                'co2': _latest['co2'],
                                'temperature': _latest['temperature'],
                                'humidity': _latest['humidity'],
                                'timestamp': time.time()
                            })
            except Exception as e:
                print(f"Sensor error: {e}", flush=True)
            time.sleep(2)

    def sendData():
        while True:
            time.sleep(30)  # Collect data for 30 seconds before averaging and sending

            data_to_send = None
            with _lock:
                if _readings:
                    # Calculate averages from collected readings
                    avg_temp = sum(r['temperature'] for r in _readings) / len(_readings)
                    avg_humidity = sum(r['humidity'] for r in _readings) / len(_readings)
                    avg_co2 = sum(r['co2'] for r in _readings) / len(_readings)

                    # Format data for backend API
                    data_to_send = {
                        "sensorId": "pi-sensor-01",
                        "temperature": round(avg_temp, 1),
                        "humidity": round(avg_humidity, 1),
                        "co2ppm": round(avg_co2, 1)
                    }

                    # Clear readings after averaging
                    _readings.clear()

            if data_to_send is not None:
                try:
                    response = requests.post(SERVER_URL, json=data_to_send, timeout=10)
                    if response.status_code == 201:
                        print(f"Averaged data sent successfully: {data_to_send}", flush=True)
                    else:
                        print(f"Error sending data: {response.status_code} - {response.text}", flush=True)
                except Exception as e:
                    print(f"Request error: {e}", flush=True)
            else:
                print("No readings collected in the last window.", flush=True)

    def main():
        poll_thread = threading.Thread(target=_poll, daemon=True)
        send_thread = threading.Thread(target=sendData, daemon=True)

        poll_thread.start()
        send_thread.start()

        print("Sensor app started. Press Ctrl+C to stop.", flush=True)
        try:
            while True:
                time.sleep(1)
        except KeyboardInterrupt:
            print("Stopping sensor app...", flush=True)


    if __name__ == "__main__":
        main()

except Exception as e:
    print(f"Fatal error: {e}", file=sys.stderr, flush=True)
    traceback.print_exc()
    sys.exit(1)
