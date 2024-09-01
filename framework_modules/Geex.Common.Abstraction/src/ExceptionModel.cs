﻿using System.Collections;

namespace Geex.Common.Abstraction;

public class ExceptionModel
{
    public string ExceptionType { get; set; }
    public string Message { get; set; }
    public string? Source { get; set; }
    public IDictionary? Data { get; set; }
}