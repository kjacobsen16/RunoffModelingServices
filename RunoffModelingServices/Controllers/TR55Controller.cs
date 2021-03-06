﻿//------------------------------------------------------------------------------
//----- HttpController ---------------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2017 WIM - USGS

//    authors:  Jeremy K. Newson USGS Web Informatics and Mapping,
//              Tara A. Gross USGS Colorado Water Science Center
//  
//   purpose:   Handles resources through the HTTP uniform interface.
//
//discussion:   Controllers are objects which handle all interaction with resources. 
//              
//
// 

using Microsoft.AspNetCore.Mvc;
using System;
using TR55Agent;
using System.Threading.Tasks;
using System.Collections.Generic;
using TR55Agent.Resources;
using System.Linq;
using Microsoft.Extensions.Options;
using RunoffModelingServices.Resources;
using RunoffModelingServices.ServiceAgents;
using WIM.Services.Attributes;

namespace RunoffModelingServices.Controllers
{
    [Route("[controller]")]
    [APIDescription(type = DescriptionType.e_string, Description = "The TR55 resource represents the NRCS TR55 hydrologic model. Resultants return the calculated peak runoff and input parameters. Tabular hydrograph variables are also returned from the Compute Hydrograph resource.")]
    public class TR55Controller : ControllerBase
    {
        public ITR55Agent agent { get; set; }
        private TDSettings tempdistSettings { get; set; }
        public TR55Controller(ITR55Agent sa, IOptions<TDSettings> settings) : base()
        {
            tempdistSettings = settings.Value;
            this.agent = sa;
        }
        #region METHODS
        //collects data from client, checks for valid precip, calls method to calculate Q
        [HttpGet(Name = "Compute TR55")]
        [APIDescription(type = DescriptionType.e_link, Description = "/Docs/TR55/compute.md")]
        public async Task<IActionResult> Get(double precip, double crvnum, string pdur)
        {
            try
            {
                if (precip < 0 || precip > 100)
                    return new BadRequestObjectResult("One or more of the parameters are invalid.");
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
            int Hlocation = pdur.IndexOf("H") - 1;
            int dur = Convert.ToInt32(pdur.Substring(1, Hlocation));
            return Ok(agent.Execute(precip, crvnum, dur));
        }
        //collects data from client, calls method to gather appropriate NOAA temporal precip distribution data for hyetograph, passes all data off to compute hydrograph values
        [HttpGet(Name = "Compute Hydrograph")]
        [APIDescription(type = DescriptionType.e_link, Description = "/Docs/TR55/compute_hydrograph.md")]
        public async Task<IActionResult> GetResult(double area, double precip, double crvnum, string pdur)  
        {
            Dictionary<double, double> hyeto = new Dictionary<double, double>();
            try
            {
                int tdx = 6;
                int tdy = 24;
                int Hlocation = pdur.IndexOf("H") - 1;
                int dur = Convert.ToInt32(pdur.Substring(1, Hlocation));
                var sa = new TDServiceAgent(this.tempdistSettings);

                if (dur == tdx || dur == tdy)
                {
                    sa = new TDServiceAgent(this.tempdistSettings);
                    var isOk = await sa.ReadTDAsync(dur);

                    if (!isOk) throw new Exception("Failed to retrieve temporal distribution data");
                }

                hyeto = sa.PassHyeto();

                if (hyeto == null)
                   return new BadRequestObjectResult("Hyetograph data incomplete.");

                return Ok(agent.ExecuteHydro(area, precip, crvnum, dur, hyeto));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
        #endregion
        #region HELPER METHODS      

    #endregion
    }
}
