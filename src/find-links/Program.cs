using FindLinks;
using FindLinks.Services;

// Usage: find-links <magazineType> [<volume>] [<number>] [-f|--force]
if (args.Length < 1 || args.Length > 4)
{
	Console.WriteLine("Usage: find-links <magazineType> [<volume>] [<number>] [-f|--force]");
	return;
}

string magazineType = args[0];
string? volume = null;
string? number = null;
bool force = false;

if (args.Length == 2)
{
	if (args[1] == "-f" || args[1] == "--force")
		force = true;
	else
		volume = args[1];
}
else if (args.Length == 3)
{
	if (args[2] == "-f" || args[2] == "--force")
	{
		volume = args[1];
		force = true;
	}
	else
	{
		volume = args[1];
		number = args[2];
	}
}
else if (args.Length == 4)
{
	volume = args[1];
	number = args[2];
	force = args[3] == "-f" || args[3] == "--force";
}

// Dependency injection
var connString = Environment.GetEnvironmentVariable("MAGAZINE_DB") ?? "Host=localhost;Username=postgres;Password=Barnowl1;Database=magazines";
using var conn = new Npgsql.NpgsqlConnection(connString);
conn.Open();
IDatabaseRepository dbRepo = new PostgresRepository(conn);
IOcrService ocrService = new TesseractOcrService();
IContentParser contentParser = new MagazineContentParser();
var linkParser = new IssueLinkParser();
var app = new FindLinksApp(dbRepo, ocrService, contentParser, linkParser);
app.Run(magazineType, volume, number, force);


