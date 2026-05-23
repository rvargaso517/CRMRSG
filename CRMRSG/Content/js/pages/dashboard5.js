$(function () {
    "use strict";
  
   var options = {
          series: [{
          name: 'OPD',
          data: [40, 130, 30, 30, 200, 105, 250, 60, 66]
        }, {
          name: 'ICU',
          data: [50, 100, 60, 200, 150, 90, 150, 114, 94]
        }],
          chart: {
          type: 'bar',
          height: 350
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
          categories: ['2010', '2011', '2012', '2013', '2014', '2015', '2016'],
        },
       
        fill: {
          opacity: 1
        },
        tooltip: {
          y: {
            formatter: function (val) {
              return "" + val + ""
            }
          }
        }
        };

        var chart = new ApexCharts(document.querySelector("#chart-1"), options);
        chart.render();


  $("span.line2").peity("line", {
    fill: ["#17b3a3"],
    stroke: ["#17b3a3"],
    height: 58,
    width: 200,
  });


  Morris.Area({
        element: 'area-chart2',
        data: [{
            period: '2013',
            data1: 0,
            data2: 0,
            
        }, {
            period: '2014',
            data1: 435,
            data2: 185,
            
        }, {
            period: '2015',
            data1: 185,
            data2: 165,
            
        }, {
            period: '2016',
            data1: 275,
            data2: 205,
            
        }, {
            period: '2023',
            data1: 585,
            data2: 455,
            
        }, {
            period: '2018',
            data1: 310,
            data2: 295,
            
        },
         {
            period: '2019',
            data1: 355,
            data2: 255,
           
        }],
        xkey: 'period',
        ykeys: ['data1', 'data2'],
        labels: ['Data 1', 'Data 2'],
        pointSize: 0,
        fillOpacity: 0.6,
        pointStrokeColors:['#f2426d', '#4d7cff'],
        behaveLikeLine: true,
        gridLineColor: '#e0e0e0',
        lineWidth: 0,
        smooth: false,
        hideHover: 'auto',
        lineColors: ['#f2426d', '#4d7cff'],
        resize: true
        
    });

  

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

