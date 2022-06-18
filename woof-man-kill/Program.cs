using WoofManKill;

switch (args[0])
{
    case "host":
        Host host = new();
        host.Start();
        break;

    case "client":
        Client.Start();
        break;

    default:
        break;
}
