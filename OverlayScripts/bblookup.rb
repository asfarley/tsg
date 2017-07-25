# bblookup.rb
# Author: Alexander Farley
# Created 1:59 PM July 16, 2017
# 
# This script maps a list of bounding-boxes to a list of object quantity (subitization). 
# The script takes two paths as input: one path to a textfile containing a list of bounding boxes, 
# another path to a folder containing a list of object position logs (statehistory.txt files). 
#
#
# Usage:
# ruby bblookup.rb E:\TSG\Test\ C:\VTCProject\vtc\bin\examples\BoundingBoxInfo.txt
require './TrajectoryBundle'
require 'pry'

#Verify command-line parameters
if(ARGV.count < 3)
	puts "bblookup requires 3 arguments:a path to the state-histories folder and a bounding-box file path."
	puts "1) path to the state-histories folder"
	puts "2) bounding-box file path"
	puts "3) output file path"
	puts "ex:"
	puts "ruby bblookup.rb ./StateHistories/ ./BoundingBoxInfo.txt ./ClassifiedBoundingBoxes.txt"
	exit
end

stateHistoriesFolderPath = ARGV[0]
boundingBoxFilePath = ARGV[1]
outputFilePath = ARGV[2]

if not File.directory? stateHistoriesFolderPath
	puts "State histories path (#{stateHistoriesFolderPath}) is not a folder."
	exit
end

if not File.exists? boundingBoxFilePath
	puts "Bounding box file (#{boundingBoxFilePath}) does not exist."
	exit
end

if File.exist?(outputFilePath)
	puts "Output file will be overwritten, continue?"
	ans = STDIN.gets
	if ans.downcase.include? "y"
		File.delete outputFilePath
	else
		puts "Exiting"
		exit
	end
end

stateCount = TrajectoryBundle.getStateHistoryCount(stateHistoriesFolderPath)
if(stateCount < 1)
	puts "No state logs found in #{stateHistoriesFolderPath}, exiting."
	exit
else
	puts "Discovered #{stateCount} state logs"
end

#Read in state logs
bundle = TrajectoryBundle.new
bundle.readFromFile(stateHistoriesFolderPath)

#Read in bounding box list
output_file = File.new(outputFilePath, "w")
File.readlines(boundingBoxFilePath).each do |line|
	quote_indices = (0 ... line.length).find_all { |i| line[i,1] == '"' }
	filepathEndIndex = quote_indices[1]
	filepath = line[quote_indices[0]..quote_indices[1]]
	boundingBoxString = line[filepathEndIndex+1..-1]
	boundingBoxElements = boundingBoxString.split " "
	x = boundingBoxElements[0].split(":")[1].to_f
	y = boundingBoxElements[1].split(":")[1].to_f
	width = boundingBoxElements[2].split(":")[1].to_i
	height = boundingBoxElements[3].split(":")[1].to_i
	frame = boundingBoxElements[4].split(":")[1].to_i

	puts "Examining frame #{frame} at location x:#{x}, y:#{y}, width:#{width}, height:#{height}"
	count = bundle.numObjectsInSubframe(x,y,width,height,frame)
	output_line = "Filename:#{filepath} X:#{x} Y:#{y} Width:#{width} Height:#{height} Frame:#{frame} Count:#{count}"
	output_file.puts output_line
end