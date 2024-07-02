# CpuAndGpuMetrics

Matrox's Automation script for testing FFMPEG with Nvidia & Intel GPUs:

- Test decoding and encoding capabilities of FFMPEG with multiple videos' specification.
- Multiple threads to test GPU's parallel capabilities.
- Create out an xml file with usage data as result.

How to use:

- place desired videos to test into the folder ~\CpuAndGpuMetrics\CROSS_PLATFORM\Encode_Decode_Testing_Automation\Encode_Decode_Testing_Automation\bin\Debug
- build the project on visual studio 2022: Build CpuAndGpuMetrics.sln
- run the Encode_Decode_Testing_Automation.sln
- select the desired option on the execution terminal
- after running, a new xml file should be created in the repo
