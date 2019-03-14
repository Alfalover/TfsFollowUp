
window.getRandomColor = function (list, number) {
    var length = Object.keys(list).length;
    var index = number % length;
    return list[Object.keys(list)[index]]

}

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
    darkGrey: 'rgb(101, 103, 107)',
    limeGreen: 'rgb(0, 255, 0)'
};

window.lineChartColors = {
    red: 'rgba(255, 99, 132,0.2)',
    pink: 'rgba(255, 23, 232,0.2)',
    orange: 'rgba(255, 159, 64,0.2)',
    yellow: 'rgba(255, 205, 86,0.2)',
    darkYellow: 'rgba(155, 105, 86,0.2)',
    green: 'rgba(75, 192, 75,0.2)',
    blue: 'rgba(54, 162, 235,0.2)',
    purple: 'rgba(153, 102, 255,0.2)',
    grey: 'rgba(201, 203, 207,0.2)',
    darkGrey: 'rgba(101, 103, 107,0.2)'
};


window.charts = {}
window.chartsConfig = {}

function getSearchParams(k) {
    var p = {};
    location.search.replace(/[?&]+([^=&]+)=([^&]*)/gi, function (s, k, v) { p[k] = v })
    return k ? p[k] : p;
}

function getStateLabels() {

    return ["New",
        "Groomable",
        "Groomed",
        "Committed",
        "Under Development",
        "PR Pending",
        "To Validate",
        "Done",
    ]
}


function mapLabelDataset(labels, source) {

    var values = [];

    labels.forEach(function (x) {
        var value = $.grep(source, function (y) { return y.name == x })
        if (value.length > 0)
            values.push(value[0].count)
        else
            values.push(0)

    });

    return values;
}

function getStateColors() {

    var names = getStateLabels();
    var result = [];
    names.forEach(function (x) {
        var color = window.chartColors.grey
        switch (x) {
            case "Committed": color = window.chartColors.red; break;
            case "New": color = window.chartColors.darkGrey; break;
            case "Under Development": color = window.chartColors.pink; break;
            case "Groomable": color = window.chartColors.yellow; break;
            case "Groomed": color = window.chartColors.orange; break;
            case "Done": color = window.chartColors.green; break;
            case "To Validate": color = window.chartColors.Grey; break;
            case "PR Pending": color = window.chartColors.darkYellow; break;
        }
        result.push(color);
    })

    return result;
}

function createLineDataset(datasetName, values, serieColor, desc) {

    return {
        data: values,
        label: datasetName,
        backgroundColor: serieColor,
        borderColor: serieColor,
        desc: desc,
        //steppedLine: true,
        lineTension: 0,
        type: 'line',
        yAxisID: 'y2',
        // pointRadius: 0,
        fill: false
        // lineTension: 0,
        // borderWidth: 2
    };

}

function createAreaDataset(datasetName, values, serieColor, desc, html, nameUrl) {

    return {
        data: values,
        label: datasetName,
        backgroundColor: serieColor,
        borderColor: serieColor,
        desc: desc,
        url: html,
        nameUrl: nameUrl,
        //steppedLine: true,
        lineTension: 0,
        type: 'line',
        yAxisID: 'y1'
        // pointRadius: 0,
        // fill: false,
        // lineTension: 0,
        // borderWidth: 2
    };

}

function hideOne(ci, index, item) {
    var meta = ci.getDatasetMeta(index);
    meta.hidden = meta.hidden === null ? !ci.data.datasets[index].hidden : null;

    if (meta.hidden) {
        $("#" + item).html('<box class="glyphicon glyphicon-eye-close"></box>');
    } else {
        $("#" + item).html("");
    }

    ci.update();
}

function hide(ci, index, item) {

    if (index == undefined) {
        var total = ci.data.datasets.length;
        for (i = 0; i < total; i++) {
            hideOne(ci, i, "cl" + i);
        }


    } else {
        hideOne(ci, index, item);
    }
}

function createLinePlot(name, names, dataset, legendDom, max) {

    showStdLegend = true;
    if (legendDom != undefined) {
        showStdLegend = false;
    }

    var tickMax = 350;
    if (max != undefined) {
        tickMax = max;
    }

    // Create new
    if (window.charts[name] == undefined) {

        var domObject = $("#" + name)[0];

        window.chartsConfig[name] = {
            type: 'line',
            data: {
                datasets: [dataset],
                labels: names
            },
            options: {
                responsive: true,
                legendCallback: function (chart) {

                    var text = [];
                    text.push('<table class="cLegend">');
                    text.push('<tr><td><box class="glyphicon glyphicon-eye-open"  OnClick="hide(window.charts[\'' + name + '\'],undefined,this)" />');
                    text.push('</td><td><span>');
                    text.push('</span></td><td/></tr>')
                    for (var i = 0; i < chart.data.datasets.length; i++) {
                        text.push('<tr><td><box id="cl' + i + '" style="background-color:' + chart.data.datasets[i].backgroundColor
                            + '" OnClick="hide(window.charts[\'' + name + '\'],' + i + ',this.id)" />');
                        text.push('</td><td><span>');
                        if (chart.data.datasets[i].nameUrl == undefined) {
                            text.push(chart.data.datasets[i].label);
                        } else {
                            text.push('<a href="' + chart.data.datasets[i].nameUrl + '">' + chart.data.datasets[i].label) + '</a>';
                        }
                        text.push('</span></td>');
                        if (chart.data.datasets[i].url != undefined) {
                            text.push('<td><a target="_blank" rel="noopener noreferrer" href=' + encodeURI(chart.data.datasets[i].url) + ' class="glyphicon glyphicon-link" /></td>')
                        }
                        text.push('</tr>');
                    }
                    text.push('</table>');
                    return text.join("");

                },
                scales: {
                    xAxes: [{
                        type: 'time',
                        distribution: 'series',
                        ticks: {
                            source: 'labels'
                        }
                    }],
                    yAxes: [
                        {
                            id: "y2",
                            stacked: false,
                            scaleLabel: {
                                display: false,
                                labelString: 'Remaining'
                            },
                            ticks: {
                                max: tickMax,
                                min: 0
                            }

                        },
                        {
                            id: "y1",
                            stacked: true,
                            position: 'right',
                            scaleLabel: {
                                display: true,
                                labelString: 'Remaining'
                            },
                            ticks: {
                                max: tickMax,
                                min: 0
                            }
                        }]
                },
                legend: {
                    display: showStdLegend,
                    position: 'right',
                    labels: {
                        boxWidth: 20,
                        padding: 5
                    }
                },
                tooltips: {
                    callbacks: {
                        label: function (tooltipItem, data) {
                            var dataset = data.datasets[tooltipItem.datasetIndex].label + ' ';
                            var name = data.datasets[tooltipItem.datasetIndex].desc;

                            var firstValue = data.datasets[tooltipItem.datasetIndex].data[0].y;
                            var value = data.datasets[tooltipItem.datasetIndex].data[tooltipItem.index].y;

                            if (tooltipItem.index > 0) {
                                var prevValue = data.datasets[tooltipItem.datasetIndex].data[tooltipItem.index - 1].y;
                            }

                            if (dataset) {
                                dataset += '-' + name + ': ';
                            }


                            dataset += value;


                            if (tooltipItem.index > 0) {
                                dataset += " iDelta: " + (value - firstValue);
                                dataset += " sDelta: " + (value - prevValue);
                            }

                            return dataset;
                        }
                    }
                }
            }
        };

        window.charts[name] = new Chart(domObject, window.chartsConfig[name]);
    } else {
        // Append dataset 		
        window.chartsConfig[name].data.datasets.push(dataset);

        function SortByName(a, b) {
            var aName = a.label.toLowerCase();
            var bName = b.label.toLowerCase();
            return ((aName < bName) ? 1 : ((aName > bName) ? -1 : 0));
        }

        //window.chartsConfig[name].data.datasets.reverse();

        window.chartsConfig[name].data.datasets.sort(SortByName);
        window.charts[name].update();

    }

    if (legendDom != undefined) {
        legend = window.charts[name].generateLegend();
        domObject = $("#" + legendDom).html(legend);
    }
}

function createPieChart(name, datasetName, values, names, colors) {


    // Create new
    if (window.charts[name] == undefined) {

        var domObject = $("#" + name)[0];

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
                rotation: -1 * Math.PI,
                legend: {
                    position: 'bottom',
                    labels: {
                        boxWidth: 20,
                        padding: 5
                    }
                },
                tooltips: {
                    callbacks: {
                        label: function (tooltipItem, data) {
                            var dataset = data.datasets[tooltipItem.datasetIndex].label || '';
                            var labeltext = data.labels[tooltipItem.index]
                            var value = data.datasets[tooltipItem.datasetIndex].data[tooltipItem.index]

                            if (dataset) {
                                dataset += '.' + labeltext + ': ';
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

        var dataset = {
            data: values,
            backgroundColor: colors,
            label: datasetName
        }

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

