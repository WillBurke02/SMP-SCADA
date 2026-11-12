# SMP-SCADA

SCADA for Bar position monitoring for Staveley Tank
Reads from a Metronix smartServo over EtherCAT using a Windows service and a WPF application

## Projects
Core: Monitoring, Alarms, Storage, Pipe protocol
Service: Windows service, EtherCAT link 
App: WPF HMI (Overview, Drive, Alarms, Engineer)
Native: ECAT Shim C wrapper

Data stored in C:\ProgramData\SMP SCADA

## Notes
Engineer Page requires a PIN, encrypted in EngineerViewModel.cs
Manuals and tools come from App/Links.txt

