# Klima-Kontrolloeren

[![Backend CI/CD](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-backend.yml/badge.svg?branch=main)](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-backend.yml)
[![Frontend CI/CD](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-frontend.yml/badge.svg?branch=main)](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-frontend.yml)
[![Test Backend CI/CD](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenbackend.yml/badge.svg?branch=Test)](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenbackend.yml)
[![Test Frontend CI/CD](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenfrontend.yml/badge.svg?branch=Test)](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenfrontend.yml)

Smart Home-system, med Raspberry-pi

## Build and Test Status

| Environment | Backend | Frontend | Deploy target |
| --- | --- | --- | --- |
| Production | [`main_klimakontrolloeren-backend.yml`](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-backend.yml) | [`main_klimakontrolloeren-frontend.yml`](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-frontend.yml) | `KlimaKontrolloeren-Backend`, `KlimaKontrolloeren-Frontend` |
| Test | [`test_testklimakontrolloerenbackend.yml`](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenbackend.yml) | [`test_testklimakontrolloerenfrontend.yml`](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenfrontend.yml) | `TestKlimaKontrolloerenBackend`, `TestKlimaKontrolloerenFrontend` |

The backend workflows publish a test summary in each GitHub Actions run, including total, passed, failed, and skipped tests for the latest build.
