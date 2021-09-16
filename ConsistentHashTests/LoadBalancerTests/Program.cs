using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using LoadBalancerTests;

var servers = JsonSerializer.Deserialize<ApiCacheServers>(File.ReadAllText("api-cache-servers.json"));

//{ "preview":false,"result":{ "request_host":"portal.vtexcommerce.com.br","request_path":"/api/catalog_system/pvt/sellers","request_querystring":"?an=paizgt&sellerType=2&sc=1"} }

var requests = File.ReadAllLines("requests.json").Select(line =>
{
    using var doc = JsonDocument.Parse(line);

    var result = doc.RootElement.GetProperty("result");
    return 
        new Request(
            result.GetProperty("request_host").GetString(), 
            result.GetProperty("request_path").GetString() + 
            result.GetProperty("request_querystring").GetString());
}).ToArray();


var consistentHash = new ConsistentHash(servers!.servers);
var map = new Dictionary<string, int>();
var random = new Random();
foreach (var request in requests)
{
    var key = consistentHash.Next(request.Host, request.PathAndQuery).endpoint.OriginalString;
    // var key = consistentHash.NextBoundedTryNext(request.Host, request.PathAndQuery).endpoint.OriginalString;
    if (map.ContainsKey(key))
    {
        map[key] += random.Next(1,8);
    }
    else
    {
        map.Add(key, 1);
    }
}

foreach(var (_, freq) in map)
    Console.WriteLine($"{freq}");

public record Request(string Host, string PathAndQuery);
