#!/usr/bin/env ruby
# This script takes a path to a folder containing PNG images and a separate
# path to a folder containing state history logs. The images are overlaid with
# bounding boxes represented in state history files for visual verification.
#
# Author::    Alexander Farley

require 'chunky_png'
require 'optparse'
require 'fileutils'
require 'pry'

# Holds per-frame state information on a tracked object.
class State
  attr_accessor :x, :y, :width, :height, :frame
  def initialize(x,y,width,height,frame)
    @x = x
    @y = y
    @width = width
    @height = height
    @frame = frame
  end
end

# Holds the entire history of states for an object as it enters and exits the scene
class StateHistory
  def initialize
    @states = []
  end
  def states
    @states
  end
  def add(state)
    @states.push(state)
  end
  def containsFrame?(frame)
    @states.each do |state|
      if(state.frame == frame)
        return true #Found match
      end
    end
    return false #No match
  end
  def stateInFrame(frame)
    @states.each do |state|
      if(state.frame == frame)
        return state
      end
    end
    return nil
  end
end

# Holds a history of states spanning all frames in a video.
class TrajectoryBundle
  def initialize
    @trajectories = []
  end
  def trajectories
    @trajectories
  end
  def containsFrame?(frame)
    #binding.pry
    @trajectories.each do |state_history|
      if(state_history.containsFrame?(frame))
        return true
      end
    end
    return false
  end
  def statesInFrame(frame)
    states = []
    @trajectories.each do |state_history|
      if(state_history.containsFrame?(frame))
        #binding.pry
        state = state_history.stateInFrame(frame)
        #puts "Found state in frame: #{state}"
        states.push(state)
      end
    end
    return states
  end
  def add(state_history)
    @trajectories.push(state_history)
  end
end

options = {}
OptionParser.new do |opts|
  opts.banner = "Usage: overlay.rb [options]"
  opts.on("-i", "--images", "Images path") do |images_path|
    options[:images_path] = images_path
  end
  opts.on("-s", "--states", "State histories path") do |states_path|
    options[:states_path] = states_path
  end
end.parse!

#p options
#p ARGV

#Read state histories
tj = TrajectoryBundle.new
Dir[ARGV[1] + "/*statehistory.txt"].each do |file|
  sh = StateHistory.new
  File.readlines(file).each do |line|
    #Parse x,y,width,height,frame
    values = line.split(" ")
    x = values[6].to_i
    y = values[7].to_i
    width = values[10].to_i
    height = values[11].to_i
    frame = values[14].to_i
    #puts "Read state X:#{x} Y:#{y} WIDTH:#{width} HEIGHT:#{height} FRAME:#{frame}"
    s = State.new(x,y,width,height,frame)
    sh.add(s)
  end
  #puts "Adding trajectory"
  tj.add(sh)
end

Dir[ARGV[0] + "/*.png"].each do |file|
  basename = File.basename(file, ".*")
  dirname = File.dirname(file)
  frame = basename.to_i

  #Open image file
  image = ChunkyPNG::Image.from_file(file)
  binding.pry
  #puts image.metadata['Title']
  #image.metadata['Author'] = 'Willem van Bergen'
  #image.save('with_metadata.png') # Overwrite file

  if(tj.containsFrame?(frame))
    states = tj.statesInFrame(frame)
    #puts "For frame ##{frame}"
    states.each do |state|
      #binding.pry
      image.rect(state.x, state.y, state.x + state.width, state.y + state.height, ChunkyPNG::Color::WHITE)
      #If bounding box is in range, overlay on image
      #puts state
    end
  end

  output_dir = File.join(dirname, "Overlay")
  FileUtils.mkdir_p output_dir
  overlay_path = File.join(output_dir, basename + "_overlay.png")

  #binding.pry
  image.save(overlay_path)
end
