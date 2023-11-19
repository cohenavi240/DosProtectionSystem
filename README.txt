Hey,

This is my solution for the assignment. Before you review it, please read these two important notes:

1. In addition to the client identifier validation assignment, I've implemented an additional check based on the client's IP address just in case they find a way to bypass the clientId validation.
For your convenience in case you run and debug the code, by default the IP address validation functionality is disabled (commented out).
If you wish to run and debug the code with the IP address validation alongside the clientId validation, comment out line 48 and un-comment line 51 in DosProtectionService.cs

2. The solution was written in C# Web API Application. As such, this type of application is stateless and cannot get user input directly (such as key press event), only in the form of HTTP requests.
To overcome this technical limit, I came up with a workaround.
I've created an HTTP endpoint (ExitController) that receives HTTP requests that will mock the key press events.
I've used an IP address validation that only allows requests coming from the server itself
to simulate the scenario you requested where only the server can press a key to exit the program.
Once the controller method fires the event, the implementation is identical to a real key pressed scenario by the server.


Thank you!