
		
		window.chartColors = {
			red: 'rgb(255, 99, 132)',
			pink: 'rgb(255, 23, 232)',
			orange: 'rgb(255, 159, 64)',
			yellow: 'rgb(255, 205, 86)',
			darkYellow: 'rgb(155, 105, 86)',
			green: 'rgb(75, 192, 75)',
			blue: 'rgb(54, 162, 235)',
			purple: 'rgb(153, 102, 255)',
			grey: 'rgb(201, 203, 207)',
			darkGrey: 'rgb(101, 103, 107)'
		};

		window.charts = {}
		window.chartsConfig = {}
		
		function getStateLabels() {
			
			return [ "New",
			         "Groomable", 	
			         "Groomed" ,
				     "Committed",					 
					 "Under Development"  	,
			         "PR Pending" 	,
			         "To Validate" ,
					 "Done" 	 	,		
			       ]  		
		}
		
		
		function mapLabelDataset(labels,source){
			
					var values= [];
					
					labels.forEach(function(x) {
						var value = $.grep(source,function(y) {return y.name == x})
						if (value.length > 0) 
							values.push(value[0].count)
						else 
							values.push(0)
							
					});
					
					return values;
		}
		
		function getStateColors(){
			
			var names = getStateLabels();
			var result= [];
			names.forEach(function(x){
										var color = window.chartColors.grey
										switch(x) {
											case "Committed" : 			color = window.chartColors.red; 		break;
											case "New"      : 			color = window.chartColors.darkGrey; 	break;
											case "Under Development" : 	color = window.chartColors.pink; 		break;
											case "Groomable" : 			color = window.chartColors.yellow; 		break;
											case "Groomed" : 			color = window.chartColors.orange; 		break;
											case "Done" :	 			color = window.chartColors.green; 		break;
											case "To Validate" :		color = window.chartColors.Grey;	 	break;
											case "PR Pending" :			color = window.chartColors.darkYellow; 	break;
										}
										result.push(color);
									})
			
			return result;
		}
		
		function createPieChart(name,datasetName, values, names, colors) {
			
			
			// Create new
			if(window.charts[name] == undefined) {
			
					var domObject= $("#"+name)[0];
			
					window.chartsConfig[name] = {
						type: 'doughnut',
						data: {
							datasets: [{
								data: values,
								backgroundColor: colors,
								label: datasetName
							}],
							labels: names
						},
						options: {
							responsive: true,
							circumference: Math.PI,
							rotation : -1 * Math.PI,
							legend: { 
								position:'bottom',
								labels : {
									boxWidth: 20,
									padding: 5									
								}
							},
							tooltips: {
								callbacks: {
									label: function(tooltipItem, data) {
										var dataset = data.datasets[tooltipItem.datasetIndex].label || '';
										var labeltext = data.labels[tooltipItem.index]
										var value   = data.datasets[tooltipItem.datasetIndex].data[tooltipItem.index]
										
										if (dataset) {
											dataset += '.'+labeltext+': ';
										}
										
										dataset += value;
										return dataset;
									}
								}
							}
						}
					};
					
					window.charts[name] = new Chart(domObject, window.chartsConfig[name]);		
			} else {
				// Append dataset 
				
				var dataset = { data:values,
								backgroundColor: colors,
								label: datasetName}
			
				window.chartsConfig[name].data.datasets.push(dataset);
				window.charts[name].update();
				
			}
					
		}

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
		
		function updateBoostrap() {
		 
					setTimeout(function () {
                        $("[data-toggle=popover]").each(function (i, obj) {

                            $(this).popover({
                                html: true,
                                content: function () {
                                    var id = $(this).attr('id')
                                    return $('#popover-content-' + id).html();
                                }
                            });
                        });

                        $('[data-toggle="tooltip"]').each(function (i, obj) {

                            $(this).tooltip({
                                animated: 'fade',
                                placement: 'bottom',
                                container: 'body'
                            });
                        });

						

                    }, 500);
		 
		 }
		
		