using System;

namespace ModelsLib;

public class Command
{
    public MyHttpMethod Method { get; set; }
    public Car? Car { get; set; }
}
