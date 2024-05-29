<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:xs="http://www.w3.org/2001/XMLSchema"
                xmlns:array="http://www.w3.org/2005/xpath-functions/array"
                xmlns:map="http://www.w3.org/2005/xpath-functions/map"
                xmlns:math="http://www.w3.org/2005/xpath-functions/math"
                xmlns:md="http://tempuri.org/horizont.pb"
                xmlns="http://tempuri.org/horizont.pb"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                xmlns:user="http://user.org"
                version="3.0">

  <xsl:output method="html" indent="yes"/>

  <msxsl:script implements-prefix="user" language="C#">
    <![CDATA[
      public string toHex (uint n)
      {
        return (n % 0x1000000).ToString("X6");
      }
     ]]>
  </msxsl:script>

  <xsl:template match="/">
    <html>
      <head>
        <!-- <link rel="stylesheet" type="text/css" href="file:///C:/XE/Projects/Device2/CreateMetaData/formats.css"></link> -->
      </head>
      <body>
        <xsl:apply-templates select="//md:struct_t[@adr]" mode="RootModule"/>
      </body>
    </html>
  </xsl:template>

  <xsl:template match="md:struct_t[@adr]" mode="RootModule">
        <div>
          device: <b style="color:teal">
            <xsl:value-of select="@name"/>
          </b>          
          <xsl:for-each select="@*[(local-name() != 'name') and (local-name() !='schemaLocation')]">
            <div style="margin-left:16px">
              <small style="color:#923800">
                <xsl:value-of select="name()"/>=<xsl:value-of select="."/>
              </small>
            </div>
          </xsl:for-each>

          <xsl:apply-templates select="./md:struct_t[@RootPath]" mode="RootStruct"/>
        </div>
  </xsl:template>
  
  <xsl:template  match="md:struct_t[@RootPath]" mode="RootStruct">
    <ul style="margin-top:4px">
      <xsl:call-template name="struct">
        <xsl:with-param name="caption" select="@RootPath"/>
      </xsl:call-template>
    </ul>
  </xsl:template>

  <xsl:template name="struct">
    <xsl:param name="caption" select="@name"/>
    <b style="color:blue">
      <xsl:value-of select="$caption"/>
      <xsl:if test="@from">
        [<xsl:value-of select="@from"/>]
      </xsl:if>
    </b>

    <xsl:call-template name="small_gray_info"/>
    <xsl:call-template name="attr"/>
    <ul class="nested">
      <xsl:for-each select="*">
        <li>
          <xsl:if test="local-name() != 'struct_t'">
            <xsl:call-template name="data"/>
          </xsl:if>
          <xsl:if test="local-name() = 'struct_t'">
            <xsl:call-template name="struct"/>
          </xsl:if>
        </li>
      </xsl:for-each>
    </ul>
  </xsl:template>

  <xsl:template name="data">
    <xsl:value-of select="local-name()"/>
    :
    <xsl:choose>
      <xsl:when test="@name">
        <xsl:choose>
          <xsl:when test="@color">
            <xsl:variable name="clr" select="user:toHex(number(@color))"/>
            <b style="color:#{$clr}">
              <xsl:value-of select="@name"/>
            </b>
          </xsl:when>
          <xsl:otherwise>
            <b>
              <xsl:value-of select="@name"/>
            </b>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:otherwise>
        <b style="color:red">noname</b>
      </xsl:otherwise>
    </xsl:choose>
    <xsl:if test="@array">
      [<xsl:value-of select="@array"/>]
    </xsl:if>
    <xsl:call-template name="small_gray_info"/>
    = <b>
      <xsl:choose>
        <xsl:when test="@arrayShowLen">          
          <xsl:value-of select="substring(text(), 0, @arrayShowLen)"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="text()"/>
        </xsl:otherwise>      
      </xsl:choose>      
      <xsl:value-of select="@eu"/>
    </b>
    <xsl:if test="@digits">
      <small style="color:gray">
        <sub>
          [<xsl:value-of select="@digits"/>:<xsl:value-of select="@precision"/>]
        </sub>
      </small>
    </xsl:if>
    <xsl:if test="@ReadOnly">
      <b>
        <small style="color:red">
          <sub>RO</sub>
        </small>
      </b>
    </xsl:if>
    <xsl:call-template name="attr"/>
  </xsl:template>

  <xsl:template name="small_gray_info">
    <small style="color:gray">
      <sub>
        <xsl:value-of select="@metr"/>
        <xsl:if test="@size">
          sz: <xsl:value-of select="@size"/>
        </xsl:if>
        p:<xsl:value-of select="@global"/>,<xsl:value-of select="@local"/>
      </sub>
    </small>
  </xsl:template>

  <xsl:template name="attr">
    <xsl:for-each select="./@*[(name() != 'tip') 
            and (name() != 'array')
            and (name() != 'precision')
            and (name() != 'digits')
            and (name() != 'global')
            and (name() != 'from')
            and (name() != 'ReadOnly')
            and (name() != 'local')
            and (name() != 'color')
            and (name() != 'RootPath')
            and (name() != 'name')
            and (name() != 'eu')
            and (name() != 'size') 
            and (name() != 'metr')]">
      <small style="color:#6CA2BB;">
        : <xsl:value-of select="name()"/>=<xsl:value-of select="."/>
      </small>
    </xsl:for-each>
  </xsl:template>

</xsl:stylesheet>


