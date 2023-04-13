using System;

namespace Models;

public class Command
{
    public HttpMethod Method { get; set; }
    public Car? Car { get; set; }
}
