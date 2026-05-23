$(function () {
    "use strict";
  
  
 var spark1 = {
    chart: {
    id: 'spark1',
    group: 'sparks',
    type: 'area',
    height: 200,
    sparkline: {
      enabled: true
    },
    dropShadow: {
      enabled: true,
      top: 5,
      left: 1,
      blur: 5,
      opacity: 0.1,
    }
    },
    series: [{
    data: [45, 54, 38, 56, 24, 65, 31, 37, 39, 62, 51, 35]
    }],
    stroke: {
    curve: 'smooth'
    },
    markers: {
    size: 0
    },
    grid: {
    padding: {
      top: 50,
      bottom: 0,
      right: 0,
      left: 0
    }
    },
    colors: ['#ff562f'],
    tooltip: {
     theme: 'dark',
    x: {
      show: false
    },
    y: {
      title: {
      formatter: function formatter(val) {
        return '';
      }
      }
    }
    }
  }
  new ApexCharts(document.querySelector("#spark1"), spark1).render();


  var spark1 = {
    chart: {
    id: 'spark1',
    group: 'sparks',
    type: 'area',
    height: 200,
    sparkline: {
      enabled: true
    },
    dropShadow: {
      enabled: true,
      top: 5,
      left: 1,
      blur: 5,
      opacity: 0.1,
    }
    },
    series: [{
    data: [45, 52, 38, 24, 33, 26, 21, 20, 6, 8, 15, 10]
    }],
    stroke: {
    curve: 'smooth'
    },
    markers: {
    size: 0
    },
    grid: {
    padding: {
      top: 50,
      bottom: 0,
      right: 0,
      left: 0
    }
    },
    colors: ['#ff9920'],
    tooltip: {
     theme: 'dark',
    x: {
      show: false
    },
    y: {
      title: {
      formatter: function formatter(val) {
        return '';
      }
      }
    }
    }
  }
  new ApexCharts(document.querySelector("#spark2"), spark1).render();


  var spark1 = {
    chart: {
    id: 'spark1',
    group: 'sparks',
    type: 'area',
    height: 200,
    sparkline: {
      enabled: true
    },
    dropShadow: {
      enabled: true,
      top: 5,
      left: 1,
      blur: 5,
      opacity: 0.1,
    }
    },
    series: [{
    data: [35, 41, 62, 42, 13, 18, 29, 37, 36, 51, 32, 35]
    }],
    stroke: {
    curve: 'smooth'
    },
    markers: {
    size: 0
    },
    grid: {
    padding: {
      top: 50,
      bottom: 0,
      right: 0,
      left: 0
    }
    },
    colors: ['#9c27b0'],
    tooltip: {
     theme: 'dark',
    x: {
      show: false
    },
    y: {
      title: {
      formatter: function formatter(val) {
        return '';
      }
      }
    }
    }
  }
  new ApexCharts(document.querySelector("#spark3"), spark1).render();


  var spark1 = {
    chart: {
    id: 'spark1',
    group: 'sparks',
    type: 'area',
    height: 200,
    sparkline: {
      enabled: true
    },
    dropShadow: {
      enabled: true,
      top: 5,
      left: 1,
      blur: 5,
      opacity: 0.1,
    }
    },
    series: [{
    data: [87, 57, 74, 99, 75, 38, 62, 47, 82, 56, 45, 47]
    }],
    stroke: {
    curve: 'smooth'
    },
    markers: {
    size: 0
    },
    grid: {
    padding: {
      top: 50,
      bottom: 0,
      right: 0,
      left: 0
    }
    },
    colors: ['#04a08b'],
    tooltip: {
     theme: 'dark',
    x: {
      show: false
    },
    y: {
      title: {
      formatter: function formatter(val) {
        return '';
      }
      }
    }
    }
  }
  new ApexCharts(document.querySelector("#spark4"), spark1).render();



    var chart = new ApexCharts(
    document.querySelector("#apexcharts-mixed"), {
      chart: {
        height: 412,
        type: "line",
        stacked: false,
         toolbar: {
        show: false,},
      },
      stroke: {
        width: [2, 3, 5],
        curve: "smooth",
        dashArray: [5, 5, 5]
      },
      plotOptions: {
        bar: {
          columnWidth: "50%"
        }
      },
      colors: ["#ff8f00", "#ee1044", "#389f99"],
      series: [{
        name: "In Stock",
        type: "area",
        data: [23, 11, 22, 27, 13, 22, 37, 21, 44, 22, 30]
      }, {
        name: "Online",
        type: "area",
        data: [44, 55, 41, 67, 22, 43, 21, 41, 56, 27, 43]
      }, {
        name: "Total Visits",
        type: "area",
        data: [30, 25, 36, 30, 45, 35, 64, 52, 59, 36, 39]
      }],
      legend: {
        position: 'top',
        horizontalAlign: 'center',
      },
      fill: {
        opacity: [0.25, 0.25, 0.25],
        gradient: {
          inverseColors: false,
          shade: "light",
          type: "vertical",
          opacityFrom: 0.85,
          opacityTo: 0.55,
          stops: [0, 100, 100, 100]
        }
      },
      labels: ["01/01/2023", "02/01/2023", "03/01/2023", "04/01/2023", "05/01/2023", "06/01/2023", "07/01/2023", "08/01/2023", "09/01/2023", "10/01/2023",
        "11/01/2023"
      ],
      markers: {
        size: 0
      },
      xaxis: {
        type: "datetime"
      },
      yaxis: {
        title: {
          text: "",
        },
        min: 0
      },
      
      tooltip: {
        shared: false,
        intersect: false,
        enabled: false,
        y: {
          formatter: function(y) {
            if (typeof y !== "undefined") {
              return y.toFixed(0) + "";
            }
            return y;
          }
        }
      }
    }
  );
  chart.render();

  
   
  
    var a = c3.generate({
        bindto: "#area-chart",
        size: { height: 350 },
        point: { r: 4 },
        color: { pattern: ["#3e8ef7", "#ff4c52"] },
        data: {
            columns: [
                ['data1', 300, 350, 300, 0, 0, 0],
              ['data2', 130, 100, 140, 200, 150, 50]

            ],
            types: { data1: "area", data2: "area-spline" }
        },
        grid: { y: { show: !0 } }
    });
  
  

  var options3 = {
          series: [43, 32, 12, 9],
          chart: {
          type: 'pie',
          width: 120,
          height: 120,
          sparkline: {
            enabled: true
          }
        },
    colors:['#4d7cff','#51ce8a','#733aeb','#f2426d'],
        stroke: {
          width: 1
        },
        tooltip: {
          fixed: {
            enabled: false
          },
        }
        };

        var chart3 = new ApexCharts(document.querySelector("#deals-chart"), options3);
        chart3.render();

var options5 = {
          series: [{
          data: [25, 66, 41, 89, 63, 25, 44, 12, 36, 9, 54]
        }],
          chart: {
          type: 'bar',
          width: 100,
          height: 70,
          sparkline: {
            enabled: true
          }
        },
        plotOptions: {
          bar: {
            columnWidth: '80%'
          }
        },
        labels: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11],
        xaxis: {
          crosshairs: {
            width: 1
          },
        },
    colors:['#ffffff'],
        tooltip: {
          fixed: {
            enabled: false
          },
          x: {
            show: false
          },
          y: {
            title: {
              formatter: function (seriesName) {
                return ''
              }
            }
          },
          marker: {
            show: false
          }
        }
        };

        var chart5 = new ApexCharts(document.querySelector("#campaign-sent-chart-4"), options5);
        chart5.render();

  
  
  var n = c3.generate({
        bindto: "#spline-chart",
        size: { height: 340 },
        color: { pattern: ["#26C6DA", "#1e88e5"] },
        data: {
            columns: [
                ['data1', 0, 2, 3.5, 0, 13, 1, 4, 1],
              ['data2', 0, 4, 0, 4, 0, 4, 0, 4]
            ],
            type: "spline"
        },
        grid: { y: { show: !0 } }
    });
  
  

    // Callback that creates and populates a data table, instantiates the line region chart, passes in the data and draws it.
    var lineRegionChart = c3.generate({
        bindto: '#line-region',
        size: { height: 350 },
        point: {
            r: 4
        },
        color: {
            pattern: ['#0bb2d4', '#3e8ef7']
        },

        // Create the data table.
        data: {
            columns: [
                ['data1', 30, 200, 100, 400, 150, 250],
              ['data2', 50, 20, 10, 40, 15, 25]
            ],
            regions: {
                'data1': [{ 'start': 1, 'end': 2, 'style': 'dashed' }, { 'start': 3 }], // currently 'dashed' style only
                'data2': [{ 'end': 3 }]
            }
        },
        grid: {
            y: {
                show: true
            }
        }
    });
  
  
  
  
  
  
  var o = c3.generate({
        bindto: "#simple-xy",
        size: { height: 350 },
        point: { r: 4 },
        color: { pattern: ["#0bb2d4", "#17b3a3"] },
        data: {
            x: "x",
            columns: [
                ['x', 30, 50, 100, 230, 300, 310],
        ['data1', 30, 200, 100, 400, 150, 250],
        ['data2', 130, 300, 200, 300, 250, 450]
            ]
        },
        grid: { y: { show: !0 } }
    });
    setTimeout(function() {
        o.load({
            columns: [
                ['data1', 100, 250, 150, 200, 100, 350]
            ]
        })
    }, 1e3), setTimeout(function() {
        o.load({
            columns: [
                ['data3', 80, 150, 100, 180, 80, 150]
            ]
        })
    }, 1500), setTimeout(function() { o.unload({ ids: "data2" }) }, 2e3)

  });


var options5 = {
          series: [{
          data: [25, 66, 41, 89, 63, 25, 44, 12, 36, 9, 54]
        }],
          chart: {
          type: 'bar',
          width: 70,
          height: 50,
          sparkline: {
            enabled: true
          }
        },
        plotOptions: {
          bar: {
            columnWidth: '80%'
          }
        },
        labels: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11],
        xaxis: {
          crosshairs: {
            width: 1
          },
        },
    colors:['#ffffff'],
        tooltip: {
          fixed: {
            enabled: false
          },
          x: {
            show: false
          },
          y: {
            title: {
              formatter: function (seriesName) {
                return ''
              }
            }
          },
          marker: {
            show: false
          }
        }
        };

        var chart5 = new ApexCharts(document.querySelector("#campaign-sent-chart"), options5);
        chart5.render();


var options5 = {
          series: [{
          data: [25, 66, 41, 89, 63, 25, 44, 12, 36, 9, 54]
        }],
          chart: {
          type: 'bar',
          width: 70,
          height: 50,
          sparkline: {
            enabled: true
          }
        },
        plotOptions: {
          bar: {
            columnWidth: '80%'
          }
        },
        labels: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11],
        xaxis: {
          crosshairs: {
            width: 1
          },
        },
    colors:['#ffffff'],
        tooltip: {
          fixed: {
            enabled: false
          },
          x: {
            show: false
          },
          y: {
            title: {
              formatter: function (seriesName) {
                return ''
              }
            }
          },
          marker: {
            show: false
          }
        }
        };

        var chart5 = new ApexCharts(document.querySelector("#campaign-sent-chart-2"), options5);
        chart5.render();



        var options5 = {
          series: [{
          data: [25, 66, 41, 89, 63, 25, 44, 12, 36, 9, 54]
        }],
          chart: {
          type: 'bar',
          width: 70,
          height: 50,
          sparkline: {
            enabled: true
          }
        },
        plotOptions: {
          bar: {
            columnWidth: '80%'
          }
        },
        labels: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11],
        xaxis: {
          crosshairs: {
            width: 1
          },
        },
    colors:['#ffffff'],
        tooltip: {
          fixed: {
            enabled: false
          },
          x: {
            show: false
          },
          y: {
            title: {
              formatter: function (seriesName) {
                return ''
              }
            }
          },
          marker: {
            show: false
          }
        }
        };

        var chart5 = new ApexCharts(document.querySelector("#campaign-sent-chart-3"), options5);
        chart5.render();




      

        var options = {
          series: [{
          name: 'Net Profit',
          data: [5, 4, 3, 7, 5, 10, 3]
        }, {
          name: 'Revenue',
          data: [3, 2, 9, 5, 4, 6, 4]
        }],
          chart: {
          type: 'bar',
          height: 230
        },
        legend: {
          show: false,
        },
        plotOptions: {
          bar: {
            horizontal: false,
            columnWidth: '55%',
            endingShape: 'rounded'
          },
        },
        dataLabels: {
          enabled: false
        },
        stroke: {
          show: true,
          width: 2,
          colors: ['transparent']
        },
        xaxis: {
          categories: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
        },
        
        fill: {
          opacity: 1
        },
        tooltip: {
          y: {
            formatter: function (val) {
              return "$ " + val + " thousands"
            }
          }
        }
        };

        var chart = new ApexCharts(document.querySelector("#basic-bar"), options);
        chart.render();



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

