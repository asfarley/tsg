# generate_spatial.rb
# Author: Alexander Farley
# Date: 2017-07-27
#
# This script converts a textfile containing bounding boxes with contained object spatial information lists
# to a bounding-box output list file for Theano network training.
#
# Each line in the bounding box file has the following form, where the repeating-format vectors between [] brackets represent individual objects contained in the line's subframe:
#
# Filename:"C:\VTCProject\vtc\bin\examples\MovingObjectSubimages\7.bmp" X:897.0 Y:314.6 Width:67 Height:39 Frame:444 Count:2  [X:914.5 Y:300.4 Width:40.2 Height:32.6] [X:886.0 Y:319.7 Width:52.6 Height:42.6]
#
# The label file has the following form:
#
#[X Y Width Height]
#"7.bmp" [914.5 300.4 40.2 32.6 886.0 319.7 52.6 42.6]
#
# Note that the above format yields *variable-length* target vectors. These target vectors
# will typically be cast into fixed-length target vectors using a maximum field size at the next
# stage in the training-set generation pipeline.
#
#
# To use this script, supply it with arguments for the input and output filepaths.
# ruby generate_spatial.rb boundingboxes.txt spatial.txt

require 'pry'

if ARGV.count != 2
	puts "This script requires 2 arguments (input file path and output file path), #{ARGV.count} arguments received"
	exit
end

bounding_box_path = ARGV[0]
output_path = ARGV[1]

if File.file? output_path
	puts "Output file already exists; delete?"
	ans = STDIN.gets
	puts "Received #{ans}"
	if ans.downcase.include? "y" 
		puts "Ok, overwriting."
		File.delete output_path
	else
		puts "Output not written, exiting."
		exit
	end
end

def parseFilenameFromLine line
	quote_split = line.split("\"")
	complete_filename = quote_split[1]
	filename = File.basename complete_filename
	return filename
end

def parseCountFromLine line
	count = parseFromLine "Count", line
	return count
end

def parseXFromLine line
	x = parseFromLine "X", line
	return x
end

def parseYFromLine line
	y = parseFromLine "Y", line
	return y
end

def parseWidthFromLine line
	width = parseFromLine "Width", line
	return width
end

def parseHeightFromLine line
	height = parseFromLine "Height", line
	return height
end

def parseFromLine word, line
	matchdata = /#{word}:[0-9]*\.*[0-9]*/.match(line)
	substring = matchdata[0]
	split = substring.split(":")
	string = split[1]
	value = string.to_f
	return value
end

def listOfElementsFromLine line
	elements = []
	count = parseCountFromLine line
	for i in 1..count do 
		element = {}
		element[:x] = parseRepeatingFromLine "X", i, line
		element[:y] = parseRepeatingFromLine "Y", i, line
		element[:width] = parseRepeatingFromLine "Width", i, line
		element[:height] = parseRepeatingFromLine "Height", i, line
		elements.push(element)
	end
	return elements
end

def parseRepeatingFromLine word, index, line
	matches = line.scan(/#{word}:[0-9]*\.*[0-9]*/)
	substring = matches[index]
	split = substring.split(":")
	string = split[1]
	value = string.to_f
	return value
end

def padElementsList list, targetLength
	nElements = list.count
	nAdded = targetLength - nElements
	padding = []
	for i in 1..nAdded do
		padElement = { :x => "0", :y => "0", :width => "0", :height => "0" }
		list << padElement
	end
	return list
end

# Do a first pass through the object-quantity file to determine the maximum number of object present in any particular subimage.
# This is necessary in order to write the header for the output file, which describes the output vector for the training set.
max_count = 0
puts "Seeking for max object count..."
File.readlines(bounding_box_path).each do |line|
	count = parseCountFromLine line
	max_count = (count > max_count) ? count : max_count
end
puts "Max object count is #{max_count}"

# open and write to a file with ruby
open(output_path, 'w') { |f|
	puts "Writing converted training examples..."
	File.readlines(bounding_box_path).each do |line|
		#Parse line
		#Line format:
		#Filename:"{Filename}" X:{X} Y:{Y} Width:{Width} Height:{Height} Frame:{Frame} Count:{Count} [X:{X} Y:{Y} Width:{Width} Height:{Height}] ...
		#First step of parsing: split off fixed-size header describing subframe bounding-box and path.
		filename = parseFilenameFromLine line
		x = parseXFromLine line
		y = parseYFromLine line
		width = parseWidthFromLine line
		height = parseHeightFromLine line
		count = parseCountFromLine line
		elements = listOfElementsFromLine line
		elements_padded = padElementsList elements, max_count
		element_substrings = []
		elements.each { |element|
			element_substring = "#{element[:x]} #{element[:y]} #{element[:width]} #{element[:height]}"
			element_substrings.push element_substring
		}
		output_vector_string = element_substrings.join " "
		f.puts "\"#{filename}\" [#{output_vector_string}]"
	end
}
puts "Done."