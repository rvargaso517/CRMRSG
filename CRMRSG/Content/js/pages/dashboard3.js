$(function () {
    "use strict";


  
  var plot = $.plot('#flotChart', [{
          data: flotSampleData3,
          color: '#06d79c',
          lines: {
            fillColor: { colors: [{ opacity: 0 }, { opacity: 1 }]}
          }
        },{
          data: flotSampleData4,
          color: '#ff562f ',
          lines: {
            fillColor: { colors: [{ opacity: 0 }, { opacity: 1 }]}
          }
        }], {
          series: {
            shadowSize: 1,
            lines: {
              show: true,
              lineWidth: 3,
              fill: true
            }
          },
          grid: {
            borderWidth: 0,
            labelMargin: 8
          },
          yaxis: {
                  show: true,
              min: 0,
              max: 70,
                  ticks: [[0,''],[20,'20K'],[40,'40K'],[60,'60K'],[80,'80K']],
                  tickColor: 'rgba(255, 255, 255, 0.10)',
            font: {
              color: '#666666'
              }
          },
          xaxis: {
                  show: true,
                  color: 'rgba(255, 255, 255, 0.10)',
                  ticks: [[25,'OCT 21'],[75,'OCT 22'],[100,'OCT 23'],[125,'OCT 24']],
            font: {
              color: '#666666'
              }
          }
        });




  var options = {
          series: [{
          data: [47, 45, 54, 38, 56, 24, 65, 31, 37, 39, 62, 51, 35, 41, 35, 27, 93, 53, 61, 27, 54, 43, 19, 46]
        }],
          chart: {
          type: 'area',
          height: 150,
          sparkline: {
            enabled: true
          },
        },
        stroke: {
          curve: 'smooth',
      width: 2,
        },
        fill: {
          opacity: 1,
          type: 'gradient',
          gradient: {
            gradientToColors: ['#843cf7', '#ec4b71']
          },
        },
        colors: ['#843cf7'],
        xaxis: {
          crosshairs: {
            width: 1
          },
        },
        yaxis: {
          min: 0
        },
        
        };
    var chart = new ApexCharts(document.querySelector("#expenses-spark"), options);
        chart.render();


 var options = {
          series: [{
          name: 'Sale',
          data: [20, 16, 24, 28, 26, 22, 15, 5, 14, 16, 22, 29, 24, 19, 15, 10, 11, 15, 19, 23]
        }, {
          name: 'Views',
          data: [20, 16, 24, 28, 26, 22, 15, 5, 14, 16, 22, 29, 24, 19, 15, 10, 11, 15, 19, 23]
        }],
          chart: {
      foreColor:"#bac0c7",
          type: 'bar',
          height: 420,
          stacked: true,
          toolbar: {
            show: false
          },
          zoom: {
            enabled: true
          }
        },
        responsive: [{
          breakpoint: 480,
          options: {
            legend: {
              position: 'bottom',
              offsetX: 0,
              offsetY: 0
            }
          }
        }],   
    grid: {
      show: true,
      borderColor: '#f7f7f7',      
    },
    colors:['#4974e0', '#e83a75'],
        plotOptions: {
          bar: {
            horizontal: false,
            columnWidth: '70%',
            endingShape: 'rounded',
        colors: {
        backgroundBarColors: ['#f0f0f000'],
        backgroundBarOpacity: 1,
      },
          },
        },
        dataLabels: {
          enabled: false
        },
 
      labels: [15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 1, 2, 3, 4],
      xaxis: {
        axisBorder: {
          show: false
        },
        axisTicks: {
          show: false
        },
        crosshairs: {
          show: false
        },
        labels: {
          show: false,
          style: {
          fontSize: '14px'
          }
        },
      },
      yaxis: {
        axisBorder: {
          show: false
        },
        labels: {
          show: false
        },
      },
        legend: {
          show: true,
      position: 'bottom',
          horizontalAlign: 'center',
        },
        fill: {
          opacity: 1
        }
        };

        var chart = new ApexCharts(document.querySelector("#charts_widget_1_chart"), options);
        chart.render();
      
    



  // ------------------------------
    // Basic bar chart
    // ------------------------------
    // based on prepared DOM, initialize echarts instance
        var myChart = echarts.init(document.getElementById('basic-bar'));

        // specify chart configuration item and data
        var option = {
                // Setup grid
                grid: {
                    left: '1%',
                    right: '2%',
                    bottom: '3%',
                    containLabel: true
                },

                // Add Tooltip
                tooltip : {
                    trigger: 'axis'
                },

                legend: {
                    data:['New User','Old User']
                },
                toolbox: {
                    show : false,
                    feature : {

                        magicType : {show: true, type: ['line', 'bar']},
                        restore : {show: true},
                        saveAsImage : {show: true}
                    }
                },
                color: ["#38649f", "#389f99"],
                calculable : true,
                xAxis : [
                    {
                        type : 'category',
                        data : ['Jan','Feb','Mar','Apr','May','Jun','July','Aug','Sept','Oct','Nov','Dec']
                    }
                ],
                yAxis : [
                    {
                        type : 'value'
                    }
                ],
                series : [
                    {
                        name:'New User',
                        type:'bar',
                        data:[7.2, 5.3, 6.1, 32.1, 23.1, 89.2, 158.4, 178.1, 36.4, 22.7, 7.1, 9.4],
                        markPoint : {
                            data : [
                                {type : 'max', name: 'Max'},
                                {type : 'min', name: 'Min'}
                            ]
                        },
                        markLine : {
                            data : [
                                {type : 'average', name: 'Average'}
                            ]
                        }
                    },
                    {
                        name:'Old User',
                        type:'bar',
                        data:[19.4, 7.9, 8.9, 27.9, 24.8, 88.1, 167.8, 197.5, 47.1, 16.7, 7.1, 1.5],
                        
                        
                    }
                ]
            };
        // use configuration item and data specified to show chart
        myChart.setOption(option);

  });

// slimScroll-------------------------------------------------
window.onload = function() {
  // Cache DOM Element
  var scrollable = $('.scrollable');
  
  // Keeping the Scrollable state separate
  var state = {
    pos: {
      lowest: 0,
      current: 0
    },
    offset: {
      top: [0, 0], //Old Offset, New Offset
    }
  }
  //
  scrollable.slimScroll({
    height: '300px',
    width: '',
    start: 'top'
  });
  //
  scrollable.slimScroll().bind('slimscrolling', function (e, pos) {
    // Update the Scroll Position and Offset
    
    // Highest Position
    state.pos.highest = pos !== state.pos.highest ?
      pos > state.pos.highest ? pos : state.pos.highest
    : state.pos.highest;
    
    // Update Offset State
    state.offset.top.push(pos - state.pos.lowest);
    state.offset.top.shift();
    
    if (state.offset.top[0] < state.offset.top[1]) {
      console.log('...Scrolling Down')
      // ... YOUR CODE ...
    } else if (state.offset.top[1] < state.offset.top[0]) {
      console.log('Scrolling Up...')
      // ... YOUR CODE ...
    } else {
      console.log('Not Scrolling')
      // ... YOUR CODE ...
    }
  });
};

window.onload = function() {
  // Cache DOM Element
  var scrollable = $('.scrollable');
  
  // Keeping the Scrollable state separate
  var state = {
    pos: {
      lowest: 0,
      current: 0
    },
    offset: {
      top: [0, 0], //Old Offset, New Offset
    }
  }
  //
  scrollable.slimScroll({
    height: '300px',
    width: '',
    start: 'top'
  });
  //
  scrollable.slimScroll().bind('slimscrolling', function (e, pos) {
    // Update the Scroll Position and Offset
    
    // Highest Position
    state.pos.highest = pos !== state.pos.highest ?
      pos > state.pos.highest ? pos : state.pos.highest
    : state.pos.highest;
    
    // Update Offset State
    state.offset.top.push(pos - state.pos.lowest);
    state.offset.top.shift();
    
    if (state.offset.top[0] < state.offset.top[1]) {
      console.log('...Scrolling Down')
      // ... YOUR CODE ...
    } else if (state.offset.top[1] < state.offset.top[0]) {
      console.log('Scrolling Up...')
      // ... YOUR CODE ...
    } else {
      console.log('Not Scrolling')
      // ... YOUR CODE ...
    }
  });
};

// slimScroll------------------------------------------------- End

  
  