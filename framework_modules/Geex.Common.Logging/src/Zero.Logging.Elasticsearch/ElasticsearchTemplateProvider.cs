using System;
using System.Collections.Generic;

namespace GeexBox.ElasticSearch.Zero.Logging.Elasticsearch
{
    internal class ElasticsearchTemplateProvider
    {
        public static object GetTemplate(Dictionary<string, string> settings, string templateMatchString)
        {
            return new
            {
                index_patterns = templateMatchString,
                settings,
                mappings = new
                {
                    dynamic_templates = new List<Object>
                    {
                        //when you use serilog as an adaptor for third party frameworks
                        //where you have no control over the log message they typically
                        //contain {0} ad infinitum, we force numeric property names to
                        //contain strings by default.
                        {
                            new
                            {
                                numerics_in_fields = new
                                {
                                    path_match = @"fields\.[\d+]$",
                                    match_pattern = "regex",
                                    mapping = new
                                    {
                                        type = "text",
                                        index = true,
                                        norms = false
                                    }
                                }
                            }
                        },
                        {
                            new
                            {
                                string_fields = new
                                {
                                    match = "*",
                                    match_mapping_type = "string",
                                    mapping = new
                                    {
                                        type = "keyword",
                                        index = true,
                                        ignore_above = 256,
                                        norms = false,
                                    }
                                }
                            }
                        },
                    },
                    properties = new Dictionary<string, object>
                    {
                        {
                            "category", new
                            {
                                type = "keyword",
                                ignore_above = 256,
                            }
                        },
                        {
                            "env", new
                            {
                                type = "keyword",
                                ignore_above = 256,
                            }
                        },
                        {
                            "eventId", new
                            {
                                properties = new Dictionary<string, object>
                                {
                                    {
                                        "id", new
                                        {
                                            type = "long"
                                        }
                                    },
                                    {
                                        "name", new
                                        {
                                            type = "keyword",
                                            ignore_above = 256,
                                        }
                                    }
                                }
                            }
                        },
                        {
                            "level", new
                            {
                                type = "keyword",
                                ignore_above = 256,
                            }
                        },
                        {
                            "message", new
                            {
                                type = "keyword",
                                ignore_above = 256,
                            }
                        },
                        {
                            "serverIp", new
                            {
                                type = "keyword",
                                ignore_above = 256,
                            }
                        },
                        {
                            "serviceName", new
                            {
                                type = "keyword",
                                ignore_above = 256,
                            }
                        },
                        {
                            "timestamp", new
                            {
                                type = "date"
                            }
                        },
                        {
                            "exceptions", new
                            {
                                type = "nested",
                                properties = new Dictionary<string, object>
                                {
                                    {"depth", new {type = "integer", index = false}},
                                    {"source", new {type = "text", index = false}},
                                    {"hResult", new {type = "integer", index = false}},
                                    {"message", new {type = "text", index = true,}},
                                    {"stackTrace", new {type = "text", index = false}},
                                }
                            }
                        },
                        {
                            "data", new
                            {
                                type = "nested",
                                properties = new Dictionary<string, object>
                                {
                                    {"query", new {type = "text", index = true, }},
                                    {"operationName", new {type = "text", index = true, }},
                                    {"queryId", new {type = "text", index = true, }},
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
