using SMP_SCADA.Core;
using SMP_SCADA.Core.Drive;
using SMP_SCADA.Service;

bool simulate = args.Contains("--simulate");

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "SMP SCADA");

string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SMP SCADA");
Directory.CreateDirectory(folder);
string database = Path.Combine(folder, simulate ? "shaft-sim.db" : "shaft.db");

var time = TimeProvider.System;
IDriveLink drive = simulate ? new SimulatedDrive(time) : new EtherCatDrive();

builder.Services.AddSingleton(time);
builder.Services.AddSingleton(drive);
builder.Services.AddSingleton(new ShaftMonitor(drive, new SqliteStore(database), time, simulate));
builder.Services.AddSingleton<CommandQueue>();
builder.Services.AddHostedService<MonitorWorker>();
if (OperatingSystem.IsWindows()) builder.Services.AddHostedService<PipeServer>();

builder.Build().Run();