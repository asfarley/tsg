# TrajectoryBundle.rb
# Author: Alexander Farley
# Created: 7:10 PM July 16, 2017
#
# This file contains the class TrajectoryBundle and associated classes.
# TrajectoryBundle is used to model a set of object trajectories where each trajectory 
# is a list of coordinates while the object is tracked.

# StateMeasurement
# A single detection of an object or blob.
# X and Y are blob centroid coordinates; width and height refer to blob geometry. Frame indicates which
# frame this detection was observed in.
require 'pry'

class StateMeasurement
	attr_reader :x, :y, :width, :height, :frame
	def initialize (x, y, width, height, frame)
		@x = x
		@y = y
		@width = width
		@height = height
		@frame = frame
	end
	
	# isContainedInBox? 
	# Takes a bounding box; returns true if this state measurement is (partially) contained within the bounding box passed in. Otherwise,
	# return false. 
	# This function is used to detect the quantity of vehicles partially contained in a particular bounding box.
	def isContainedInBox?(bounding_box)
		left_upper_corner = [ bounding_box["X"] - bounding_box["Width"]/2, bounding_box["Y"] - bounding_box["Height"]/2]
		if isPointInBox? left_upper_corner
			return true
		end
		
		right_upper_corner = [ bounding_box["X"] + bounding_box["Width"]/2, bounding_box["Y"] - bounding_box["Height"]/2]
		if isPointInBox? right_upper_corner
			return true
		end
		
		left_lower_corner = [ bounding_box["X"] - bounding_box["Width"]/2, bounding_box["Y"] + bounding_box["Height"]/2]
		if isPointInBox? left_lower_corner
			return true
		end
		
		right_lower_corner = [ bounding_box["X"] + bounding_box["Width"]/2, bounding_box["Y"] + bounding_box["Height"]/2]
		if isPointInBox? right_lower_corner
			return true
		end
		
		return false
	end
	
	# isPointInBox?
	# Takes a point (an array: [20.0, 31.2]  and returns true if the point is contained
	# within the rectangle indicated by this measurement.
	def isPointInBox?(point)
		min_x = @x - @width/2
		max_x = @x + @width/2
		min_y = @y - @height/2
		max_y = @y + @height/2
		if point[0] > min_x and point[0] < max_x and point[1] > min_y and point[1] < max_y
			return true
		end
		return false
	end
end

class Trajectory
	attr_accessor :measurements
	def initialize
		@measurements = []
	end
end

class TrajectoryBundle
	attr_accessor :trajectories
	def initialize
		@trajectories = []
	end

	def containsFrame?(frame)
		@trajectories.each do |t|
			t.measurements.each do |m|
				if(m.frame == frame && m.x > 0 && m.y > 0)
					return true
				end
			end
		end
		return false
	end
	
	def objectsInFrame(frame)
		objects = []
		@trajectories.each do |t|
			t.measurements.each do |m|
				if(m.frame == frame)
					objects.push(m)
				end
			end
		end
		return objects
	end
	
	def self.getStateHistoryCount(path)
		stateFilesWildcardPath = File.join(path, "*statehistory.txt").gsub('\\','/')
		stateCount = Dir[stateFilesWildcardPath].count
		return stateCount
	end
	
	def readFromFile(path)
		stateFilesWildcardPath = File.join(path, "*statehistory.txt").gsub('\\','/')
		Dir[stateFilesWildcardPath].each do |file|
			t = Trajectory.new
			File.readlines(file).each do |line|
				variables = line.split(' ')
				x = variables[6].to_f
				y = variables[7].to_f
				width = variables[10].to_f
				height = variables[11].to_f
				frame = variables[15].to_i
				m = StateMeasurement.new(x,y,width,height,frame)
				puts "Adding measurement: X=#{m.x}, Y=#{m.y}, width=#{m.width}, height=#{m.height}, frame=#{m.frame}"
				t.measurements.push(m)
			end
			puts "Adding trajectory with #{t.measurements.count} measurements"
			@trajectories.push(t)
		end
	end
	
	def numObjectsInSubframe(x,y,width,height,frame)
		num_objects = 0
		@trajectories.each do |t|
			t.measurements.each do |m|
				if(m.frame == frame)
					bounding_box = { "X" => x, "Y" => y, "Width" => width, "Height" => height, "Frame" => frame }
					if m.isContainedInBox? bounding_box
						num_objects += 1
					end
				end
			end
		end
		return num_objects
	end
end