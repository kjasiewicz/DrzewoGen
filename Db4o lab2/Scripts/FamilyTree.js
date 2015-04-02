$(document).ready(function () {
    var id = $("#idforajax").text();
    var $$ = go.GraphObject.make;  // for conciseness in defining templates
    var myDiagram =
      $$(go.Diagram, "myDiagram",  // must be the ID or reference to div
        {
            allowCopy: false,
            layout:  // create a TreeLayout for the family tree
              $$(go.TreeLayout,
                { angle: 90, nodeSpacing: 5 })
        });


    // Set up a Part as a legend, and place it directly on the diagram
    myDiagram.add(
      $$(go.Part, "Table",
        { position: new go.Point(350, 10), selectable: false },
        $$(go.TextBlock, "Key",
          { row: 0, font: "bold 10pt Helvetica, Arial, sans-serif" }),  // end row 0
        $$(go.Panel, "Horizontal",
          { row: 1, alignment: go.Spot.Left },
          $$(go.Shape, "Rectangle",
            { desiredSize: new go.Size(30, 30), fill:"lightblue",margin: 5 }),
          $$(go.TextBlock, "Mężczyźni",
            { font: "bold 8pt Helvetica, bold Arial, sans-serif" })
        ),  // end row 1
        $$(go.Panel, "Horizontal",
          { row: 2, alignment: go.Spot.Left },
          $$(go.Shape, "Rectangle",
            { desiredSize: new go.Size(30, 30),fill:"pink", margin: 5 }),
          $$(go.TextBlock, "Kobiety",
            { font: "bold 8pt Helvetica, bold Arial, sans-serif" })
        )  // end row 2
      ));

    // get tooltip text from the object's data
    function tooltipTextConverter(person) {
        var str = "";
        str += "Urodzony/a: " + person.birthYear;
        if (person.deathYear !== undefined) str += "\nZmarł/a: " + person.deathYear;
        return str;
    }

    // define tooltips for nodes
    var tooltiptemplate =
      $$(go.Adornment, "Auto",
        $$(go.Shape, "Rectangle",
          { fill: "whitesmoke", stroke: "black" }),
        $$(go.TextBlock,
          {
              font: "bold 8pt Helvetica, bold Arial, sans-serif",
              wrap: go.TextBlock.WrapFit,
              margin: 5
          },
          new go.Binding("text", "", tooltipTextConverter))
      );

    // define Converters to be used for Bindings
    function genderBrushConverter(gender) {
        if (gender === "M") return "lightblue";
        if (gender === "F") return "pink";
        return "orange";
    }

    // replace the default Node template in the nodeTemplateMap
    myDiagram.nodeTemplate =
      $$(go.Node, "Auto",
        { deletable: false, toolTip: tooltiptemplate },
        new go.Binding("text", "name"),
        $$(go.Shape, "Rectangle",
          {
              fill: "lightgray",
              stroke: "black",
              stretch: go.GraphObject.Fill,
              alignment: go.Spot.Center
          },
          new go.Binding("fill", "gender", genderBrushConverter)),
        $$(go.TextBlock,
          {
              font: "bold 8pt Helvetica, bold Arial, sans-serif",
              alignment: go.Spot.Center,
              margin: 6
          },
          new go.Binding("text", "name"))
      );

    $.getJSON("/Home/GetFamilyTreeData/" + id, {    
    }).done(function(data) {
        // create the model for the family tree
        myDiagram.model = new go.TreeModel(data);
    });

    

});