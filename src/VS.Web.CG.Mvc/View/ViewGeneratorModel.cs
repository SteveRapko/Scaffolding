// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.View
{
    public class ViewGeneratorModel : CommonCommandLineModel
    {
        public string ViewName { get; set; }

        [Argument(Description = "The view template to use, supported view templates: 'Empty|Create|Edit|Delete|Details|List'")]
        public string TemplateName { get; set; }

        [Option(Name = "partialView", ShortName = "partial", Description = "Generate a partial view, other layout options (-l and -udl) are ignored if this is specified")]
        public bool PartialView { get; set; }

        [Option(Name = "bootstrapVersion", ShortName = "b", Description = "Specify the bootstrap version. Valid values: '3', '4'. Default is 4.")]
        public string BootstrapVersion { get; set; }

        public ViewGeneratorModel()
        {
        }

        protected ViewGeneratorModel(ViewGeneratorModel copyFrom)
            : base(copyFrom)
        {
            ViewName = copyFrom.ViewName;
            TemplateName = copyFrom.TemplateName;
            PartialView = copyFrom.PartialView;
            BootstrapVersion = copyFrom.BootstrapVersion;
        }

        public override CommonCommandLineModel Clone()
        {
            return new ViewGeneratorModel(this);
        }
    }
}