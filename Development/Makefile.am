
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG;TRACE" "-keyfile:Gurux.Communication.snk"
ASSEMBLY = bin/Debug/Gurux.Communication.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Debug/

GURUX_COMMUNICATION_DLL_MDB_SOURCE=bin/Debug/Gurux.Communication.dll.mdb
GURUX_COMMUNICATION_DLL_MDB=$(BUILD_DIR)/Gurux.Communication.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:TRACE" "-keyfile:Gurux.Communication.snk"
ASSEMBLY = bin/Release/Gurux.Communication.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release/

GURUX_COMMUNICATION_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(GURUX_COMMUNICATION_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(GURUX_COMMUNICATION_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	ByteOrder.cs \
	Checksum.cs \
	Common/GXConverter.cs \
	CRCChecksum.cs \
	GXChecksum.cs \
	GXClient.cs \
	GXDelegates.cs \
	GXPacket.cs \
	GXServerReceiver.cs \
	GXServerSender.cs \
	GXStatistics.cs \
	IGXPacketParser.cs \
	PacketParts.cs \
	PacketStates.cs \
	Properties/AssemblyInfo.cs \
	GXServer.cs \
	Shared/GXCommon.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	Gurux.Communication.snk \
	gurux.communication.pc.in 

REFERENCES =  \
	$(GURUXCOMMON_LIBS) \
	System \
	System.Core \
	System.Runtime.Serialization \
	System.Windows.Forms \
	System.Xml.Linq \
	System.Data.DataSetExtensions \
	System.Data \
	System.Xml

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

GURUX_COMMUNICATION_PC = $(BUILD_DIR)/gurux.communication.pc

$(eval $(call emit-deploy-wrapper,GURUX_COMMUNICATION_PC,gurux.communication.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
