namespace Torvin.Tests;

// ApiFactory postavlja env varijable za cijeli proces i briše ih kad se ugasi. Klase koje podižu API zato idu
// jedna za drugom: inače jedna klasa obriše varijable dok druga upravo pokreće svoj host
[CollectionDefinition(Name)]
public sealed class ApiCollection
{
    public const string Name = "Api";
}
