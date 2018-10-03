
        function progressBar(min, max, value, dangerLower, dangerHigher, warningLower, warningHigher) {

            var pvalue = (Math.abs(value - min) / (max - min)) * 100;
            var tvalue = Number(value).toFixed(1)

            if (pvalue > 100) pvalue = 100;

			var color = "progress-bar-success"
			
			if (warningHigher !== undefined) {
				if (value > warningHigher)
                    color = "progress-bar-warning"				
			}
			
			if (warningLower !== undefined) {
					
				if (value < warningLower)
                    color = "progress-bar-warning"
			}
			
            if (dangerLower !== undefined) {
                if (value < dangerLower)
                    color = "progress-bar-danger"
               
            }
			
			if (dangerHigher !== undefined) {
				if (value > dangerHigher)
                    color = "progress-bar-danger"
			}
			
			

            return "<div class=\"progress bgprogress\">" +
                '<div class="wrapper"><div class=\"progress-bar progress-bar-striped active ' + color + ' bg-dark\" role=\"progressbar\" ' +
                "aria-valuenow=\"" + value + "\" aria-valuemin=\"" + min + "\" aria-valuemax=\"" + max + "\" style=\"width:" + pvalue + "%\"/><div class='progress-bar progress-bar-text position-reset'>"
                + tvalue + "%</div></div></div>"

        }
		
		