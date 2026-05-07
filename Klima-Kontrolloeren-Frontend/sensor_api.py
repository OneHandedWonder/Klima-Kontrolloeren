#!/usr/bin/env python3
from flask import Flask, jsonify
from flask_cors import CORS
import scd30_i2c
import threading
import time

app = Flask(__name__)
CORS(app)

sensor = scd30_i2c.SCD30()
sensor.set_measurement_interval(2)
sensor.start_periodic_measurement()

_latest = {'co2': None, 'temperature': None, 'humidity': None}
_lock = threading.Lock()

def _poll():
    while True:
        try:
            if sensor.get_data_ready():
                m = sensor.read_measurement()
                if m is not None:
                    co2, temp, hum = m
                    with _lock:
                        _latest['co2'] = round(float(co2), 1)
                        _latest['temperature'] = round(float(temp), 1)
                        _latest['humidity'] = round(float(hum), 1)
        except Exception as e:
            print(f"Sensor error: {e}", flush=True)
        time.sleep(2)

threading.Thread(target=_poll, daemon=True).start()

@app.route('/klima-data')
def klima_data():
    with _lock:
        data = _latest.copy()
    if data['co2'] is None:
        return jsonify({'error': 'Sensor warming up, try again shortly'}), 503
    return jsonify({
        'co2': data['co2'],
        'temperature': data['temperature'],
        'humidity': data['humidity'],
        'feelsLike': None,
        'weatherDesc': None,
        'forecast': None
    })

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
