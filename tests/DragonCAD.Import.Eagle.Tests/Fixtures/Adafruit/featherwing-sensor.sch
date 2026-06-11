<?xml version="1.0" encoding="utf-8"?>
<eagle version="9.6.2">
  <drawing>
    <schematic>
      <layers>
        <layer number="91" name="Nets" />
        <layer number="95" name="Names" />
      </layers>
      <libraries>
        <library name="Adafruit">
          <symbols>
            <symbol name="FEATHER">
              <pin name="SDA" x="0" y="0" length="short" />
              <pin name="SCL" x="0" y="-2.54" length="short" />
              <text x="0" y="2.54" size="1.27" layer="95">&gt;NAME</text>
            </symbol>
          </symbols>
        </library>
      </libraries>
      <parts>
        <part name="U1" library="Adafruit" deviceset="FEATHER" device="" />
        <part name="U2" library="Adafruit" deviceset="SENSOR" device="" />
      </parts>
      <instances>
        <instance part="U1" gate="G$1" x="10.16" y="10.16" />
        <instance part="U2" gate="G$1" x="30.48" y="10.16" />
      </instances>
      <nets>
        <net name="SDA" class="0">
          <segment>
            <pinref part="U1" gate="G$1" pin="SDA" />
            <wire x1="10.16" y1="10.16" x2="20.32" y2="10.16" width="0.1524" layer="91" />
            <wire x1="20.32" y1="10.16" x2="30.48" y2="10.16" width="0.1524" layer="91" />
          </segment>
        </net>
        <net name="SCL" class="0">
          <segment>
            <pinref part="U1" gate="G$1" pin="SCL" />
            <wire x1="10.16" y1="7.62" x2="30.48" y2="7.62" width="0.1524" layer="91" />
          </segment>
        </net>
      </nets>
      <plain>
        <text x="2.54" y="2.54" size="1.27" layer="95">Adafruit FeatherWing Sensor</text>
        <bus name="I2C" />
      </plain>
    </schematic>
  </drawing>
</eagle>
